namespace Orleans.Serialization
{
    using System;

    using Orleans.Runtime;

    [Serializable]
    [Hagar.GenerateSerializer]
    public class ILGenerationException : OrleansException
    {
        public ILGenerationException()
        {
        }

        public ILGenerationException(string message)
            : base(message)
        {
        }

        public ILGenerationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}