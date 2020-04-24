namespace Orleans.Runtime
{
    internal class GrainContextAccessor : IGrainContextAccessor
    {
        public IGrainContext GrainContext => RuntimeContext.CurrentGrainContext;
    }
}
