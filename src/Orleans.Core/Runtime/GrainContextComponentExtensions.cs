using System;
using Orleans.Runtime;

namespace Orleans
{
    public static class GrainContextComponentExtensions
    {
        private static readonly Func<IGrainContext, Type, object> CreateComponent = (ctx, type) => ctx.ActivationServices.GetRequiredServiceByKey<Type, object>(type);

        public static TComponent GetOrSetComponent<TComponent>(this IGrainContext context) => context.GetOrSetComponent<TComponent>(CreateComponent);

        public static TComponent GetOrSetComponent<TComponent>(this IGrainContext context, Func<IGrainContext, Type, object> createComponent)
        {
            var result = context.GetComponent<TComponent>();
            if (result == null)
            {
                result = (TComponent)createComponent(context, typeof(TComponent));
                context.SetComponent(result);
            }

            return result;
        }
    }
}