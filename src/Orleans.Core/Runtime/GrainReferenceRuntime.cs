using Hagar.Invocation;
using Orleans.CodeGeneration;
using Orleans.GrainReferences;
using Orleans.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.Runtime
{
    internal class GrainReferenceRuntime : IGrainReferenceRuntime
    {
        private readonly GrainReferenceActivator referenceActivator;
        private readonly GrainInterfaceTypeResolver interfaceTypeResolver;
        private readonly IGrainCancellationTokenRuntime cancellationTokenRuntime;
        private readonly IOutgoingGrainCallFilter[] filters;
        private readonly Hagar.DeepCopier deepCopier;

        public GrainReferenceRuntime(
            IRuntimeClient runtimeClient,
            IGrainCancellationTokenRuntime cancellationTokenRuntime,
            IEnumerable<IOutgoingGrainCallFilter> outgoingCallFilters,
            GrainReferenceActivator referenceActivator,
            GrainInterfaceTypeResolver interfaceTypeResolver,
            Hagar.DeepCopier deepCopier)
        {
            this.RuntimeClient = runtimeClient;
            this.cancellationTokenRuntime = cancellationTokenRuntime;
            this.referenceActivator = referenceActivator;
            this.interfaceTypeResolver = interfaceTypeResolver;
            this.filters = outgoingCallFilters.ToArray();
            this.deepCopier = deepCopier;
        }

        public IRuntimeClient RuntimeClient { get; private set; }

        /// <inheritdoc />
        public void InvokeOneWayMethod(GrainReference reference, int methodId, object[] arguments, InvokeMethodOptions options)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<T> InvokeMethodAsync<T>(GrainReference reference, int methodId, object[] arguments, InvokeMethodOptions options)
        {
            throw new NotSupportedException();
        }

        public void SendRequest(GrainReference reference, IResponseCompletionSource callback, IInvokable body, InvokeMethodOptions options)
        {
            SetGrainCancellationTokensTarget(reference, body);
            var copy = this.deepCopier.Copy(body);
            if (this.filters?.Length > 0)
            {
                //return InvokeWithFilters(reference, copy, options);
            }

            this.RuntimeClient.SendRequest(reference, copy, callback, options);
        }

        public object Cast(IAddressable grain, Type grainInterface)
        {
            var grainId = grain.GetGrainId();
            if (grain is GrainReference && grainInterface.IsAssignableFrom(grain.GetType()))
            {
                return grain;
            }

            var interfaceType = this.interfaceTypeResolver.GetGrainInterfaceType(grainInterface);
            return this.referenceActivator.CreateReference(grainId, interfaceType);
        }

        /// <summary>
        /// Sets target grain to the found instances of type GrainCancellationToken
        /// </summary>
        private void SetGrainCancellationTokensTarget(GrainReference target, IInvokable request)
        {
            for (var i = 0; i < request.ArgumentCount; i++)
            {
                var arg = request.GetArgument<object>(i);
                if (arg is not GrainCancellationToken grainToken)
                {
                    continue;
                }

                grainToken.AddGrainReference(this.cancellationTokenRuntime, target);
            }
        }
    }
}