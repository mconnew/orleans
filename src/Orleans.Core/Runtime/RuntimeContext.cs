using System;
using System.Threading.Tasks;

namespace Orleans.Runtime
{
    internal class RuntimeContext
    {
        public ISchedulingContext ActivationContext { get; private set; }

        [ThreadStatic]
        private static RuntimeContext context;
        public static RuntimeContext Current => context ??= new RuntimeContext();

        internal static ISchedulingContext CurrentActivationContext => Current.ActivationContext;

        internal static void InitializeThread()
        {
            // There seems to be an implicit coupling of threads and contexts here that may be fragile. 
            // E.g. if InitializeThread() is mistakenly called on a wrong thread, would that thread be considered a worker pool thread from that point on? 
            // Is there a better/safer way to identify worker threads? 
            if (context != null)
            {
                // Currently only orleans own threads are being initialized,
                // but in perspective - this method will be called on external ones,
                // so that mechanism of thread de-initialization will be needed.
                return; 
            }

            context = new RuntimeContext();
        }

        internal static void SetExecutionContext(ISchedulingContext shedContext)
        {
            Current.ActivationContext = shedContext;
        }

        internal static void ResetExecutionContext()
        {
            Current.ActivationContext = null;
        }

        public override string ToString()
        {
            return String.Format("RuntimeContext: ActivationContext={0}", 
                ActivationContext != null ? ActivationContext.ToString() : "null");
        }
    }
}
