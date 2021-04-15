using System;

namespace FakeFx.Runtime
{
    [Serializable]
    [Orleans.Serialization.GenerateSerializer]
    [Orleans.Serialization.WellKnownId(103)]
    [Orleans.Serialization.SuppressReferenceTracking]
    internal sealed class Response
    {
        [Orleans.Serialization.Id(1)]
        public bool ExceptionFlag { get; private set; }

        [Orleans.Serialization.Id(2)]
        public Exception Exception { get; private set; }

        [Orleans.Serialization.Id(3)]
        public object Data { get; private set; }

        public Response(object data)
        {
            switch (data)
            {
                case Exception exception:
                    Exception = exception;
                    ExceptionFlag = true;
                    break;
                default:
                    Data = data;
                    ExceptionFlag = false;
                    break;
            }
        }

        private Response()
        {
        }

        static public Response ExceptionResponse(Exception exc)
        {
            return new()
            {
                ExceptionFlag = true,
                Exception = exc
            };
        }

        public override string ToString()
        {
            if (ExceptionFlag)
            {
                return $"Response Exception={Exception}";
            }

            return $"Response Data={Data}";
        }
    }
}
