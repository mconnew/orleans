using Orleans.CodeGeneration;
using Orleans.GrainReferences;
using Orleans.Metadata;
using Orleans.Serialization;
using Orleans.Serialization.Invocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.Runtime
{
    internal class GrainReferenceRuntime : IGrainReferenceRuntime
    {
        private readonly GrainReferenceActivator referenceActivator;
        private readonly GrainInterfaceTypeResolver interfaceTypeResolver;
        private readonly IGrainCancellationTokenRuntime cancellationTokenRuntime;
        private readonly IOutgoingGrainCallFilter[] filters;
        private readonly Action<GrainReference, IResponseCompletionSource, IInvokable, InvokeMethodOptions> sendRequest;
        private readonly DeepCopier deepCopier;

        public GrainReferenceRuntime(
            IRuntimeClient runtimeClient,
            IGrainCancellationTokenRuntime cancellationTokenRuntime,
            IEnumerable<IOutgoingGrainCallFilter> outgoingCallFilters,
            GrainReferenceActivator referenceActivator,
            GrainInterfaceTypeResolver interfaceTypeResolver,
            DeepCopier deepCopier)
        {
            this.RuntimeClient = runtimeClient;
            this.cancellationTokenRuntime = cancellationTokenRuntime;
            this.referenceActivator = referenceActivator;
            this.interfaceTypeResolver = interfaceTypeResolver;
            this.filters = outgoingCallFilters.ToArray();
            this.sendRequest = this.SendRequest;
            this.deepCopier = deepCopier;
        }

        public IRuntimeClient RuntimeClient { get; private set; }

        public void SendRequest(GrainReference reference, IResponseCompletionSource callback, IInvokable body, InvokeMethodOptions options)
        {
            SetGrainCancellationTokensTarget(reference, body);
            this.RuntimeClient.SendRequest(reference, body, callback, options);
        }

        public ValueTask<TResult> InvokeMethodAsync<TResult>(GrainReference reference, IInvokable request, InvokeMethodOptions options)
        {
            // TODO: Remove expensive interface type check
            if (this.filters.Length == 0 && request is not IOutgoingGrainCallFilter)
            {
                SetGrainCancellationTokensTarget(reference, request);
                var copy = this.deepCopier.Copy(request);
                var responseCompletionSource = ResponseCompletionSourcePool.Get<TResult>();
                SendRequest(reference, responseCompletionSource, copy, options);
                return responseCompletionSource.AsValueTask();
            }
            else
            {
                return InvokeMethodWithFiltersAsync<TResult>(reference, request, options);
            }
        }

        public ValueTask InvokeMethodAsync(GrainReference reference, IInvokable request, InvokeMethodOptions options)
        {
            // TODO: Remove expensive interface type check
            if (filters.Length == 0 && request is not IOutgoingGrainCallFilter)
            {
                SetGrainCancellationTokensTarget(reference, request);
                var copy = this.deepCopier.Copy(request);
                var responseCompletionSource = ResponseCompletionSourcePool.Get();
                SendRequest(reference, responseCompletionSource, copy, options);
                return responseCompletionSource.AsVoidValueTask();
            }
            else
            {
                return new(InvokeMethodWithFiltersAsync<object>(reference, request, options).AsTask());
            }
        }

        private async ValueTask<TResult> InvokeMethodWithFiltersAsync<TResult>(GrainReference reference, IInvokable request, InvokeMethodOptions options)
        {
            SetGrainCancellationTokensTarget(reference, request);
            var copy = this.deepCopier.Copy(request);
            var invoker = new OutgoingCallInvoker<TResult>(reference, copy, options, this.sendRequest, this.filters);
            await invoker.Invoke();
            return invoker.Response.GetResult<TResult>();
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