
using System;
using System.Buffers;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Orleans.CodeGeneration;
using Orleans.Networking.Shared;
using Orleans.Serialization;
using Orleans.Transactions;

namespace Orleans.Runtime
{
    internal class MessageFactory : IPooledObjectPolicy<Message>
    {
        private readonly SerializationManager _serializationManager;
        private readonly ILogger _logger;
        private readonly MessagingTrace _messagingTrace;
        private readonly ObjectPool<Message> _messagePool;
        private readonly MemoryPool<byte> _memoryPool;

// TODO: decouple serialization from messaging at least a bit...
        private ThreadLocal<SerializationContext> _serializationContext;
        private ThreadLocal<DeserializationContext> _deserializationContext;

        public MessageFactory(
            SerializationManager serializationManager,
            ILogger<MessageFactory> logger,
            MessagingTrace messagingTrace,
            ObjectPoolProvider poolProvider,
            SharedMemoryPool memoryPool)
        {
            _serializationManager = serializationManager;
            _logger = logger;
            _messagingTrace = messagingTrace;
            _memoryPool = memoryPool.Pool;

            _messagePool = poolProvider.Create<Message>(this);
            _serializationContext = new ThreadLocal<SerializationContext>(() => new SerializationContext(_serializationManager));
            _deserializationContext = new ThreadLocal<DeserializationContext>(() => new DeserializationContext(_serializationManager));
        }

        public Message CreateMessageForDeserialization() => _messagePool.Get();

        public Message CreateMessage(InvokeMethodRequest request, InvokeMethodOptions options)
        {
            var message = _messagePool.Get();
            message.Category = Message.Categories.Application;
            message.Direction = (options & InvokeMethodOptions.OneWay) != 0 ? Message.Directions.OneWay : Message.Directions.Request;
            message.Id = CorrelationId.GetNext();
            message.IsReadOnly = (options & InvokeMethodOptions.ReadOnly) != 0;
            message.IsUnordered = (options & InvokeMethodOptions.Unordered) != 0;
            message.IsAlwaysInterleave = (options & InvokeMethodOptions.AlwaysInterleave) != 0;
            message.RequestContextData = RequestContextExtensions.Export(_serializationManager);

            message.SetBodyObject(request);

            if (options.IsTransactional())
            {
                SetTransaction(message, options);
            }
            else
            {
                // clear transaction info if not in transaction
                message.RequestContextData?.Remove(TransactionContext.Orleans_TransactionContext_Key);
            }

            _messagingTrace.OnCreateMessage(message);
            return message;
        }

        private void SetTransaction(Message message, InvokeMethodOptions options)
        {
            // clear transaction info if transaction operation requires new transaction.
            ITransactionInfo transactionInfo = TransactionContext.GetTransactionInfo();

            // enforce join transaction calls
            if (options.IsTransactionOption(InvokeMethodOptions.TransactionJoin) && transactionInfo == null)
            {
                throw new NotSupportedException("Call cannot be made outside of a transaction.");
            }

            // enforce not allowed transaction calls
            if (options.IsTransactionOption(InvokeMethodOptions.TransactionNotAllowed) && transactionInfo != null)
            {
                throw new NotSupportedException("Call cannot be made within a transaction.");
            }

            // clear transaction context if creating a transaction or transaction is suppressed
            if (options.IsTransactionOption(InvokeMethodOptions.TransactionCreate) ||
                options.IsTransactionOption(InvokeMethodOptions.TransactionSuppress))
            {
                transactionInfo = null;
            }

            bool isTransactionRequired = options.IsTransactionOption(InvokeMethodOptions.TransactionCreate) ||
                                         options.IsTransactionOption(InvokeMethodOptions.TransactionCreateOrJoin) ||
                                         options.IsTransactionOption(InvokeMethodOptions.TransactionJoin);

            message.TransactionInfo = transactionInfo?.Fork();
            message.IsTransactionRequired = isTransactionRequired;
            if (transactionInfo == null)
            {
                // if we're leaving a transaction context, make sure it's been cleared from the request context.
                message.RequestContextData?.Remove(TransactionContext.Orleans_TransactionContext_Key);
            }
        }

        public Message CreateResponseMessage(Message request)
        {
            var response = _messagePool.Get();
            response.Category = request.Category;
            response.Direction = Message.Directions.Response;
            response.Id = request.Id;
            response.IsReadOnly = request.IsReadOnly;
            response.IsAlwaysInterleave = request.IsAlwaysInterleave;
            response.TargetSilo = request.SendingSilo;
            response.TraceContext = request.TraceContext;
            response.TransactionInfo = request.TransactionInfo;

            if (request.SendingGrain != null)
            {
                response.TargetGrain = request.SendingGrain;
                if (request.SendingActivation != null)
                {
                    response.TargetActivation = request.SendingActivation;
                }
            }

            response.SendingSilo = request.TargetSilo;
            if (request.TargetGrain != null)
            {
                response.SendingGrain = request.TargetGrain;
                if (request.TargetActivation != null)
                {
                    response.SendingActivation = request.TargetActivation;
                }
                else if (request.TargetGrain.IsSystemTarget())
                {
                    response.SendingActivation = ActivationId.GetDeterministic(request.TargetGrain);
                }
            }

            response.CacheInvalidationHeader = request.CacheInvalidationHeader;
            response.TimeToLive = request.TimeToLive;

            var contextData = RequestContextExtensions.Export(_serializationManager);
            if (contextData != null)
            {
                response.RequestContextData = contextData;
            }

            _messagingTrace.OnCreateMessage(response);
            return response;
        }

        public Message CreateRejectionResponse(Message request, Message.RejectionTypes type, string info, Exception ex = null)
        {
            var response = CreateResponseMessage(request);
            response.Result = Message.ResponseTypes.Rejection;
            response.RejectionType = type;
            response.RejectionInfo = info;
            response.SetBodyObject(ex);

            if (_logger.IsEnabled(LogLevel.Debug)) _logger.Debug("Creating {0} rejection with info '{1}' for {2} at:" + Environment.NewLine + "{3}", type, info, this, Utils.GetStackTrace());
            return response;
        }

        public Message CreateErrorResponse(Message request, Exception exception)
        {
            var response = CreateResponseMessage(request);
            response.Result = Message.ResponseTypes.Error;
            response.SetBodyObject(Response.ExceptionResponse(exception));
            return response;
        }

        public T GetPayload<T>(OwnedSequence<byte> payload)
        {
            var context = _deserializationContext.Value;
            if (!(context.StreamReader is BinaryTokenStreamReader2 reader))
            {
                context.StreamReader = reader = new BinaryTokenStreamReader2();
            }

            reader.PartialReset(payload.AsReadOnlySequence);
            try
            {
                return (T)SerializationManager.DeserializeInner(_serializationManager, typeof(T), context, reader);
            }
            finally
            {
                context.Reset();
            }
        }

        public void SetPayload<T>(OwnedSequence<byte> payload, T value)
        {
            payload.Reset();

            var context = _serializationContext.Value;
            if (context.StreamWriter is BinaryTokenStreamWriter2<OwnedSequence<byte>> writer)
            {
                writer.PartialReset(payload);
            }
            else
            {
                context.StreamWriter = writer = new BinaryTokenStreamWriter2<OwnedSequence<byte>>(payload);
            }

            try
            {
                SerializationManager.SerializeInner(_serializationManager, value, typeof(T), context, writer);
                writer.Commit();
            }
            finally
            {
                context.Reset();
            }
        }

        Message IPooledObjectPolicy<Message>.Create() => new Message(this, _memoryPool);

        bool IPooledObjectPolicy<Message>.Return(Message obj) => true;

        internal void ReturnMessageToPool(Message message)
        {
            _messagePool.Return(message);
        }
    }
}