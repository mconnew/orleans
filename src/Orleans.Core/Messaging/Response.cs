using System;

namespace Orleans.Runtime
{
    [Serializable]
    internal class Response
    {
        public bool ExceptionFlag { get; private set; }
        public object Data { get; private set; }

        public Response(object data)
        {
            switch (data)
            {
                case Exception exception:
                    Data = exception;
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

        public static Response CreateResponse(object value)
        {
            return new Response
            {
                Data = value
            };
        }

        public static Response ExceptionResponse(Exception exc)
        {
            return new Response
            {
                ExceptionFlag = true,
                Data = exc
            };
        }

        public override string ToString()
        {
            if (ExceptionFlag)
            {
                return $"Response Exception={Data}";
            }

            return $"Response Data={Data}";
        }
    }
}
