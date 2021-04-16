using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Orleans.CodeGeneration;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Serialization.Invocation;
using Orleans.Transactions;

namespace Orleans
{
    /// <summary>
    /// The TransactionAttribute attribute is used to mark methods that start and join transactions.
    /// </summary>
    [InvokableCustomInitializer("SetTransactionOptions")]
    [InvokableBaseType(typeof(NewGrainReference), typeof(ValueTask), typeof(TransactionRequest))]
    [InvokableBaseType(typeof(NewGrainReference), typeof(ValueTask<>), typeof(TransactionRequest<>))]
    [InvokableBaseType(typeof(NewGrainReference), typeof(Task), typeof(TransactionTaskRequest))]
    [InvokableBaseType(typeof(NewGrainReference), typeof(Task<>), typeof(TransactionTaskRequest<>))]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TransactionAttribute : Attribute
    {
        public TransactionAttribute(TransactionOption requirement)
        {
            Requirement = requirement;
            ReadOnly = false;
        }

        public TransactionAttribute(TransactionOptionAlias alias)
        {
            Requirement = (TransactionOption)(int)alias;
            ReadOnly = false;
        }

        public TransactionOption Requirement { get; }
        public bool ReadOnly { get; set; }
    }

    public enum TransactionOption
    {
        Suppress,     // Logic is not transactional but can be called from within a transaction.  If called within the context of a transaction, the context will not be passed to the call.
        CreateOrJoin, // Logic is transactional.  If called within the context of a transaction, it will use that context, else it will create a new context.
        Create,       // Logic is transactional and will always create a new transaction context, even if called within an existing transaction context.
        Join,         // Logic is transactional but can only be called within the context of an existing transaction.
        Supported,    // Logic is not transactional but supports transactions.  If called within the context of a transaction, the context will be passed to the call.
        NotAllowed    // Logic is not transactional and cannot be called from within a transaction.  If called within the context of a transaction, it will throw a not supported exception.
    }

    public enum TransactionOptionAlias
    {
        Suppress     = TransactionOption.Supported,
        Required     = TransactionOption.CreateOrJoin,
        RequiresNew  = TransactionOption.Create,
        Mandatory    = TransactionOption.Join,
        Never        = TransactionOption.NotAllowed,
    }

    internal static class TransactionRequestHelper
    {
    }

    [Orleans.GenerateSerializer]
    public abstract class TransactionRequestBase : RequestBase, IOutgoingGrainCallFilter
    {
        private Serializer<OrleansTransactionAbortedException> _serializer = null;
        private ITransactionAgent _transactionAgent = null;

        [Id(1)]
        public TransactionOption TransactionOption { get; set; }

        public bool IsAmbientTransactionSuppressed => TransactionOption switch
        {
            TransactionOption.Create => true,
            TransactionOption.Suppress => true,
            _ => false
        };

        public bool IsTransactionRequired => TransactionOption switch
        {
            TransactionOption.Create => true,
            TransactionOption.CreateOrJoin => true,
            TransactionOption.Join => true,
            _ => false
        };

        protected void SetTransactionOptions(TransactionOptionAlias txOption) => SetTransactionOptions((TransactionOption)txOption);

        protected void SetTransactionOptions(TransactionOption txOption)
        {
            this.TransactionOption = txOption;
        }

        public async Task Invoke(IOutgoingGrainCallContext context)
        {
            var transactionInfo = SetTransactionInfo();
            try
            {
                await context.Invoke();
            }
            finally
            {
                var returnedTransactionInfo = TransactionContext.GetTransactionInfo();
                if (transactionInfo is { } && returnedTransactionInfo is { })
                {
                    transactionInfo.Join(returnedTransactionInfo);
                }
            }
        }

        private TransactionInfo SetTransactionInfo()
        { 
            // clear transaction info if transaction operation requires new transaction.
            var transactionInfo = TransactionContext.GetTransactionInfo();

            // enforce join transaction calls
            if (TransactionOption == TransactionOption.Join && transactionInfo == null)
            {
                throw new NotSupportedException("Call cannot be made outside of a transaction.");
            }

            // enforce not allowed transaction calls
            if (TransactionOption == TransactionOption.NotAllowed && transactionInfo != null)
            {
                throw new NotSupportedException("Call cannot be made within a transaction.");
            }

            transactionInfo = transactionInfo?.Fork();
            if (transactionInfo == null)
            {
                // if we're leaving a transaction context, make sure it's been cleared from the request context.
                TransactionContext.Clear();
            }
            else
            {
                TransactionContext.SetTransactionInfo(transactionInfo);
            }

            return transactionInfo;
        }

        public override async ValueTask<Response> Invoke()
        {
            Response response;
            var transactionInfo = TransactionContext.GetTransactionInfo();
            bool startNewTransaction = false;
            try
            {
                if (IsTransactionRequired && transactionInfo == null)
                {
                    // TODO: this should be a configurable parameter
                    var transactionTimeout = Debugger.IsAttached ? TimeSpan.FromMinutes(30) : TimeSpan.FromSeconds(10);

                    // Start a new transaction
                    var isReadOnly = (this.Options | InvokeMethodOptions.ReadOnly) == InvokeMethodOptions.ReadOnly;
                    transactionInfo = await this._transactionAgent.StartTransaction(isReadOnly, transactionTimeout);
                    startNewTransaction = true;
                }

                if (transactionInfo != null)
                {
                    TransactionContext.SetTransactionInfo(transactionInfo);
                }

                response = await BaseInvoke();
            }
            catch (Exception exception)
            {
                response = Response.FromException(exception);

                // Record reason for abort, if not already set.
                transactionInfo?.RecordException(exception, _serializer);
            }

            if (transactionInfo != null)
            {
                transactionInfo.ReconcilePending();
                OrleansTransactionException transactionException = transactionInfo.MustAbort(_serializer);

                // This request started the transaction, so we try to commit before returning,
                // or if it must abort, tell participants that it aborted
                if (startNewTransaction)
                {
                    try
                    {
                        if (transactionException is null)
                        {
                            var (status, exception) = await _transactionAgent.Resolve(transactionInfo);
                            if (status != TransactionalStatus.Ok)
                            {
                                transactionException = status.ConvertToUserException(transactionInfo.Id, exception);
                            }
                        }
                        else
                        {
                            await _transactionAgent.Abort(transactionInfo);
                        }
                    }
                    finally
                    {
                        TransactionContext.Clear();
                    }
                }

                if (transactionException != null)
                {
                    return Response.FromException(transactionException);
                }
            }

            return response;
        }

        protected abstract ValueTask<Response> BaseInvoke();
    }

    public abstract class TransactionRequest : TransactionRequestBase 
    {
        protected override ValueTask<Response> BaseInvoke()
        {
            try
            {
                var resultTask = InvokeInner();
                if (resultTask.IsCompleted)
                {
                    resultTask.GetAwaiter().GetResult();
                    return new ValueTask<Response>(Response.Completed);
                }

                return CompleteInvokeAsync(resultTask);
            }
            catch (Exception exception)
            {
                return new ValueTask<Response>(Response.FromException(exception));
            }
        }

        private static async ValueTask<Response> CompleteInvokeAsync(ValueTask resultTask)
        {
            try
            {
                await resultTask;
                return Response.Completed;
            }
            catch (Exception exception)
            {
                return Response.FromException(exception);
            }
        }

        // Generated
        protected abstract ValueTask InvokeInner();
    }

    public abstract class TransactionRequest<TResult> : TransactionRequestBase
    {
        protected override ValueTask<Response> BaseInvoke()
        {
            try
            {
                var resultTask = InvokeInner();
                if (resultTask.IsCompleted)
                {
                    return new ValueTask<Response>(Response.FromResult(resultTask.Result));
                }

                return CompleteInvokeAsync(resultTask);
            }
            catch (Exception exception)
            {
                return new ValueTask<Response>(Response.FromException(exception));
            }
        }

        private static async ValueTask<Response> CompleteInvokeAsync(ValueTask<TResult> resultTask)
        {
            try
            {
                var result = await resultTask;
                return Response.FromResult(result);
            }
            catch (Exception exception)
            {
                return Response.FromException(exception);
            }
        }

        // Generated
        protected abstract ValueTask<TResult> InvokeInner();
    }

    public abstract class TransactionTaskRequest<TResult> : TransactionRequestBase
    {
        protected override ValueTask<Response> BaseInvoke()
        {
            try
            {
                var resultTask = InvokeInner();
                var status = resultTask.Status;
                if (resultTask.IsCompleted)
                {
                    return new ValueTask<Response>(Response.FromResult(resultTask.GetAwaiter().GetResult()));
                }

                return CompleteInvokeAsync(resultTask);
            }
            catch (Exception exception)
            {
                return new ValueTask<Response>(Response.FromException(exception));
            }
        }

        private static async ValueTask<Response> CompleteInvokeAsync(Task<TResult> resultTask)
        {
            try
            {
                var result = await resultTask;
                return Response.FromResult(result);
            }
            catch (Exception exception)
            {
                return Response.FromException(exception);
            }
        }

        // Generated
        protected abstract Task<TResult> InvokeInner();
    }

    public abstract class TransactionTaskRequest : TransactionRequestBase 
    {
        protected override ValueTask<Response> BaseInvoke()
        {
            try
            {
                var resultTask = InvokeInner();
                var status = resultTask.Status;
                if (resultTask.IsCompleted)
                {
                    resultTask.GetAwaiter().GetResult();
                    return new ValueTask<Response>(Response.Completed);
                }

                return CompleteInvokeAsync(resultTask);
            }
            catch (Exception exception)
            {
                return new ValueTask<Response>(Response.FromException(exception));
            }
        }

        private static async ValueTask<Response> CompleteInvokeAsync(Task resultTask)
        {
            try
            {
                await resultTask;
                return Response.Completed;
            }
            catch (Exception exception)
            {
                return Response.FromException(exception);
            }
        }

        // Generated
        protected abstract Task InvokeInner();
    }
}