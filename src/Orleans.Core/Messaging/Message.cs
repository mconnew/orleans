using System;
using System.Collections.Generic;
using System.Text;
using Hagar.Invocation;
using Orleans.CodeGeneration;

namespace Orleans.Runtime
{
    [Hagar.WellKnownId(101)]
    internal sealed class Message
    {
        public const int LENGTH_HEADER_SIZE = 8;
        public const int LENGTH_META_HEADER = 4;

        public Directions _direction;
        public Categories _category;
        public bool _isReadOnly;
        public bool _isAlwaysInterleave;

        public bool _isUnordered;
        public bool _isNewPlacement;
        public ResponseTypes _result;
        public RejectionTypes _rejectionType;

        public ushort _interfaceVersion;
        public CorrelationId _id;
        public int _forwardCount;
        public SiloAddress _targetSilo;
        public GrainId _targetGrain;
        public ActivationId _targetActivation;
        public SiloAddress _sendingSilo;
        public GrainId _sendingGrain;
        public ActivationId _sendingActivation;
        public GrainInterfaceType interfaceType;
        public long _timeToLive;
        public List<ActivationAddress> _cacheInvalidationHeader;
        public string _rejectionInfo;
        public Dictionary<string, object> _requestContextData;
        public long _localCreationTime = Environment.TickCount64;

        public object BodyObject { get; set; }

        internal void Initialize()
        {
            _localCreationTime = Environment.TickCount64;
        }

        internal void Reset()
        {
            BodyObject = default;
            targetAddress = default;
            sendingAddress = default;
            _targetHistory = default;
            _retryCount = default;
            _queuedTime = default;

            _direction = default;
            _category = default;
            _isReadOnly = default;
            _isAlwaysInterleave = default;
            _isUnordered = default;
            _isNewPlacement = default;
            _result = default;
            _rejectionType = default;
            _interfaceVersion = default;
            _id = default;
            _forwardCount = default;
            _targetSilo = default;
            _targetGrain = default;
            _targetActivation = default;
            _sendingSilo = default;
            _sendingGrain = default;
            _sendingActivation = default;
            interfaceType = default;
            _timeToLive = default;
            _cacheInvalidationHeader = default;
            _rejectionInfo = default;
            _requestContextData = default;
        }

        [NonSerialized]
        private string _targetHistory;

        [NonSerialized]
        private DateTime? _queuedTime;

        [NonSerialized]
        private int _retryCount;

        // Cache values of TargetAddess and SendingAddress as they are used very frequently
        [NonSerialized]
        private ActivationAddress targetAddress;

        [NonSerialized]
        private ActivationAddress sendingAddress;

        public string TargetHistory
        {
            get { return _targetHistory; }
            set { _targetHistory = value; }
        }

        public DateTime? QueuedTime
        {
            get { return _queuedTime; }
            set { _queuedTime = value; }
        }

        public int RetryCount
        {
            get { return _retryCount; }
            set { _retryCount = value; }
        }

        [Hagar.GenerateSerializer]
        public enum Categories : byte
        {
            Ping,
            System,
            Application,
        }

        [Hagar.GenerateSerializer]
        public enum Directions : byte
        {
            None,
            Request,
            Response,
            OneWay
        }

        [Hagar.GenerateSerializer]
        public enum ResponseTypes : byte
        {
            Success,
            Error,
            Rejection,
            Status
        }

        [Hagar.GenerateSerializer]
        public enum RejectionTypes : byte
        {
            Transient,
            Overloaded,
            DuplicateRequest,
            Unrecoverable,
            GatewayTooBusy,
            CacheInvalidation
        }

        public Categories Category
        {
            get { return _category; }
            set { _category = value; }
        }

        public Directions Direction
        {
            get { return _direction; }
            set { _direction = value; }
        }

        public bool HasDirection => _direction != Directions.None;

        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set { _isReadOnly = value; }
        }

        public bool IsAlwaysInterleave
        {
            get { return _isAlwaysInterleave; }
            set { _isAlwaysInterleave = value; }
        }

        public bool IsUnordered
        {
            get { return _isUnordered; }
            set { _isUnordered = value; }
        }

        public CorrelationId Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public int ForwardCount
        {
            get { return _forwardCount; }
            set { _forwardCount = value; }
        }

        public SiloAddress TargetSilo
        {
            get { return _targetSilo; }
            set
            {
                _targetSilo = value;
                targetAddress = null;
            }
        }

        public GrainId TargetGrain
        {
            get { return _targetGrain; }
            set
            {
                _targetGrain = value;
                targetAddress = null;
            }
        }

        public ActivationId TargetActivation
        {
            get { return _targetActivation; }
            set
            {
                _targetActivation = value;
                targetAddress = null;
            }
        }

        public bool IsFullyAddressed => TargetSilo is object && !TargetGrain.IsDefault && TargetActivation is object;

        public ActivationAddress TargetAddress
        {
            get
            {
                if (targetAddress is object) return targetAddress;
                if (!TargetGrain.IsDefault)
                {
                    return targetAddress = ActivationAddress.GetAddress(TargetSilo, TargetGrain, TargetActivation);
                }

                return null;
            }

            set
            {
                TargetGrain = value.Grain;
                TargetActivation = value.Activation;
                TargetSilo = value.Silo;
                targetAddress = value;
            }
        }

        public SiloAddress SendingSilo
        {
            get { return _sendingSilo; }
            set
            {
                _sendingSilo = value;
                sendingAddress = null;
            }
        }

        public GrainId SendingGrain
        {
            get { return _sendingGrain; }
            set
            {
                _sendingGrain = value;
                sendingAddress = null;
            }
        }

        public ActivationId SendingActivation
        {
            get { return _sendingActivation; }
            set
            {
                _sendingActivation = value;
                sendingAddress = null;
            }
        }

        public ActivationAddress SendingAddress
        {
            get { return sendingAddress ?? (sendingAddress = ActivationAddress.GetAddress(SendingSilo, SendingGrain, SendingActivation)); }
            set
            {
                SendingGrain = value.Grain;
                SendingActivation = value.Activation;
                SendingSilo = value.Silo;
                sendingAddress = value;
            }
        }

        public bool IsNewPlacement
        {
            get { return _isNewPlacement; }
            set
            {
                _isNewPlacement = value;
            }
        }

        public ushort InterfaceVersion
        {
            get { return _interfaceVersion; }
            set
            {
                _interfaceVersion = value;
            }
        }

        public GrainInterfaceType InterfaceType
        {
            get { return interfaceType; }
            set
            {
                interfaceType = value;
            }
        }

        public ResponseTypes Result
        {
            get { return _result; }
            set { _result = value; }
        }

        public TimeSpan? TimeToLive
        {
            get => _timeToLive == 0 ? default(TimeSpan?) : TimeSpan.FromMilliseconds(_timeToLive - (Environment.TickCount64 - _localCreationTime));
            set => _timeToLive = !value.HasValue ? 0 : (long)(value.Value.TotalMilliseconds + (Environment.TickCount64 - _localCreationTime));
        }

        public bool IsExpired
        {
            get
            {
                if (!TimeToLive.HasValue)
                    return false;

                return TimeToLive <= TimeSpan.Zero;
            }
        }

        public bool IsExpirableMessage(bool dropExpiredMessages)
        {
            if (!dropExpiredMessages) return false;

            GrainId id = TargetGrain;
            if (id.IsDefault) return false;

            // don't set expiration for one way, system target and system grain messages.
            return Direction != Directions.OneWay && !id.IsSystemTarget();
        }

        public List<ActivationAddress> CacheInvalidationHeader
        {
            get { return _cacheInvalidationHeader; }
            set { _cacheInvalidationHeader = value; }
        }

        public bool HasCacheInvalidationHeader => this.CacheInvalidationHeader != null
                                                  && this.CacheInvalidationHeader.Count > 0;

        internal void AddToCacheInvalidationHeader(ActivationAddress address)
        {
            var list = new List<ActivationAddress>();
            if (CacheInvalidationHeader != null)
            {
                list.AddRange(CacheInvalidationHeader);
            }

            list.Add(address);
            CacheInvalidationHeader = list;
        }

        public RejectionTypes RejectionType
        {
            get { return _rejectionType; }
            set { _rejectionType = value; }
        }

        public string RejectionInfo
        {
            get { return GetNotNullString(_rejectionInfo); }
            set { _rejectionInfo = value; }
        }

        public Dictionary<string, object> RequestContextData
        {
            get { return _requestContextData; }
            set { _requestContextData = value; }
        }

        public void ClearTargetAddress()
        {
            targetAddress = null;
        }

        private static string GetNotNullString(string s)
        {
            return s ?? string.Empty;
        }

        /// <summary>
        /// Tell whether two messages are duplicates of one another
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsDuplicate(Message other)
        {
            return Equals(SendingSilo, other.SendingSilo) && Equals(Id, other.Id);
        }

        // For testing and logging/tracing
        public string ToLongString()
        {
            var sb = new StringBuilder();

            AppendIfExists(Headers.CACHE_INVALIDATION_HEADER, sb, (m) => m.CacheInvalidationHeader);
            AppendIfExists(Headers.CATEGORY, sb, (m) => m.Category);
            AppendIfExists(Headers.DIRECTION, sb, (m) => m.Direction);
            AppendIfExists(Headers.TIME_TO_LIVE, sb, (m) => m.TimeToLive);
            AppendIfExists(Headers.FORWARD_COUNT, sb, (m) => m.ForwardCount);
            AppendIfExists(Headers.CORRELATION_ID, sb, (m) => m.Id);
            AppendIfExists(Headers.ALWAYS_INTERLEAVE, sb, (m) => m.IsAlwaysInterleave);
            AppendIfExists(Headers.IS_NEW_PLACEMENT, sb, (m) => m.IsNewPlacement);
            AppendIfExists(Headers.READ_ONLY, sb, (m) => m.IsReadOnly);
            AppendIfExists(Headers.IS_UNORDERED, sb, (m) => m.IsUnordered);
            AppendIfExists(Headers.REJECTION_INFO, sb, (m) => m.RejectionInfo);
            AppendIfExists(Headers.REJECTION_TYPE, sb, (m) => m.RejectionType);
            AppendIfExists(Headers.REQUEST_CONTEXT, sb, (m) => m.RequestContextData);
            AppendIfExists(Headers.RESULT, sb, (m) => m.Result);
            AppendIfExists(Headers.SENDING_ACTIVATION, sb, (m) => m.SendingActivation);
            AppendIfExists(Headers.SENDING_GRAIN, sb, (m) => m.SendingGrain);
            AppendIfExists(Headers.SENDING_SILO, sb, (m) => m.SendingSilo);
            AppendIfExists(Headers.TARGET_ACTIVATION, sb, (m) => m.TargetActivation);
            AppendIfExists(Headers.TARGET_GRAIN, sb, (m) => m.TargetGrain);
            AppendIfExists(Headers.TARGET_SILO, sb, (m) => m.TargetSilo);

            return sb.ToString();
        }

        private void AppendIfExists(Headers header, StringBuilder sb, Func<Message, object> valueProvider)
        {
            // used only under log3 level
            if ((GetHeadersMask() & header) != Headers.NONE)
            {
                sb.AppendFormat("{0}={1};", header, valueProvider(this));
                sb.AppendLine();
            }
        }

        public override string ToString()
        {
            string response = String.Empty;
            if (Direction == Directions.Response)
            {
                switch (Result)
                {
                    case ResponseTypes.Error:
                        response = "Error ";
                        break;

                    case ResponseTypes.Rejection:
                        response = string.Format("{0} Rejection (info: {1}) ", RejectionType, RejectionInfo);
                        break;

                    case ResponseTypes.Status:
                        response = "Status ";
                        break;

                    default:
                        break;
                }
            }
            return String.Format("{0}{1}{2}{3}{4} {5}->{6}{7} #{8}{9}",
                IsReadOnly ? "ReadOnly " : "", //0
                IsAlwaysInterleave ? "IsAlwaysInterleave " : "", //1
                IsNewPlacement ? "NewPlacement " : "", // 2
                response,  //3
                Direction, //4
                $"[{SendingSilo} {SendingGrain} {SendingActivation}]", //5
                $"[{TargetSilo} {TargetGrain} {TargetActivation}]", //6
                BodyObject is InvokeMethodRequest request ? $" {request.ToString()}" : string.Empty, // 7
                Id, //8
                ForwardCount > 0 ? "[ForwardCount=" + ForwardCount + "]" : ""); //9
        }

        public string GetTargetHistory()
        {
            var history = new StringBuilder();
            history.Append("<");
            if (TargetSilo != null)
            {
                history.Append(TargetSilo).Append(":");
            }
            if (!TargetGrain.IsDefault)
            {
                history.Append(TargetGrain).Append(":");
            }
            if (TargetActivation is object)
            {
                history.Append(TargetActivation);
            }
            history.Append(">");
            if (!string.IsNullOrEmpty(TargetHistory))
            {
                history.Append("    ").Append(TargetHistory);
            }
            return history.ToString();
        }

        public static Message CreatePromptExceptionResponse(Message request, Exception exception)
        {
            return new Message
            {
                Category = request.Category,
                Direction = Message.Directions.Response,
                Result = Message.ResponseTypes.Error,
                BodyObject = Response.FromException(exception)
            };
        }

        [Flags]
        public enum Headers
        {
            NONE = 0,
            ALWAYS_INTERLEAVE = 1 << 0,
            CACHE_INVALIDATION_HEADER = 1 << 1,
            CATEGORY = 1 << 2,
            CORRELATION_ID = 1 << 3,
            DEBUG_CONTEXT = 1 << 4, // No longer used
            DIRECTION = 1 << 5,
            TIME_TO_LIVE = 1 << 6,
            FORWARD_COUNT = 1 << 7,
            NEW_GRAIN_TYPE = 1 << 8,
            GENERIC_GRAIN_TYPE = 1 << 9,
            RESULT = 1 << 10,
            REJECTION_INFO = 1 << 11,
            REJECTION_TYPE = 1 << 12,
            READ_ONLY = 1 << 13,
            RESEND_COUNT = 1 << 14, // Support removed. Value retained for backwards compatibility.
            SENDING_ACTIVATION = 1 << 15,
            SENDING_GRAIN = 1 << 16,
            SENDING_SILO = 1 << 17,
            IS_NEW_PLACEMENT = 1 << 18,

            TARGET_ACTIVATION = 1 << 19,
            TARGET_GRAIN = 1 << 20,
            TARGET_SILO = 1 << 21,
            TARGET_OBSERVER = 1 << 22,
            IS_UNORDERED = 1 << 23,
            REQUEST_CONTEXT = 1 << 24,
            INTERFACE_VERSION = 1 << 26,

            INTERFACE_TYPE = 1 << 31
            // Do not add over int.MaxValue of these.
        }

        internal Headers GetHeadersMask()
        {
            Headers headers = Headers.NONE;
            if (Category != default(Categories))
                headers = headers | Headers.CATEGORY;

            headers = _direction == Directions.None ? headers & ~Headers.DIRECTION : headers | Headers.DIRECTION;
            if (IsReadOnly)
                headers = headers | Headers.READ_ONLY;
            if (IsAlwaysInterleave)
                headers = headers | Headers.ALWAYS_INTERLEAVE;
            if (IsUnordered)
                headers = headers | Headers.IS_UNORDERED;

            headers = _id.ToInt64() == 0 ? headers & ~Headers.CORRELATION_ID : headers | Headers.CORRELATION_ID;

            if (_forwardCount != default(int))
                headers = headers | Headers.FORWARD_COUNT;

            headers = _targetSilo == null ? headers & ~Headers.TARGET_SILO : headers | Headers.TARGET_SILO;
            headers = _targetGrain.IsDefault ? headers & ~Headers.TARGET_GRAIN : headers | Headers.TARGET_GRAIN;
            headers = _targetActivation is null ? headers & ~Headers.TARGET_ACTIVATION : headers | Headers.TARGET_ACTIVATION;
            headers = _sendingSilo is null ? headers & ~Headers.SENDING_SILO : headers | Headers.SENDING_SILO;
            headers = _sendingGrain.IsDefault ? headers & ~Headers.SENDING_GRAIN : headers | Headers.SENDING_GRAIN;
            headers = _sendingActivation is null ? headers & ~Headers.SENDING_ACTIVATION : headers | Headers.SENDING_ACTIVATION;
            headers = _isNewPlacement == default(bool) ? headers & ~Headers.IS_NEW_PLACEMENT : headers | Headers.IS_NEW_PLACEMENT;
            headers = _interfaceVersion == 0 ? headers & ~Headers.INTERFACE_VERSION : headers | Headers.INTERFACE_VERSION;
            headers = _result == default(ResponseTypes) ? headers & ~Headers.RESULT : headers | Headers.RESULT;
            headers = _timeToLive == 0 ? headers & ~Headers.TIME_TO_LIVE : headers | Headers.TIME_TO_LIVE;
            headers = _cacheInvalidationHeader == null || _cacheInvalidationHeader.Count == 0 ? headers & ~Headers.CACHE_INVALIDATION_HEADER : headers | Headers.CACHE_INVALIDATION_HEADER;
            headers = _rejectionType == default(RejectionTypes) ? headers & ~Headers.REJECTION_TYPE : headers | Headers.REJECTION_TYPE;
            headers = string.IsNullOrEmpty(_rejectionInfo) ? headers & ~Headers.REJECTION_INFO : headers | Headers.REJECTION_INFO;
            headers = _requestContextData == null || _requestContextData.Count == 0 ? headers & ~Headers.REQUEST_CONTEXT : headers | Headers.REQUEST_CONTEXT;
            headers = interfaceType.IsDefault ? headers & ~Headers.INTERFACE_TYPE : headers | Headers.INTERFACE_TYPE;
            return headers;
        }
    }
}
