using System;
using Orleans.Runtime;

namespace Orleans
{
    public static class GrainContextComponentExtensions
    {
        private static readonly Func<IGrainContext, Type, object> CreateExtension = (ctx, type) =>
            {
                var result = ctx.ActivationServices.GetServiceByKey<Type, IGrainExtension>(type);
                if (result is null) throw new GrainExtensionNotInstalledException($"A grain extension of type {type} is not installed on grain {ctx}");
                return result;
            };

        public static TComponent GetGrainExtension<TComponent>(this IGrainContext context)
            where TComponent : IGrainExtension
            => context.GetOrSetComponent<TComponent>(CreateExtension);

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