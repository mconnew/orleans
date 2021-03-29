using System;
using System.Reflection;
using System.Threading.Tasks;
using Hagar.Invocation;
using Orleans.CodeGeneration;

namespace Orleans.Runtime
{
    /// <summary>
    /// Invokes a request on a grain reference.
    /// </summary>
    internal class OutgoingCallInvoker<TResult> : IOutgoingGrainCallContext
    {
        private readonly IInvokable request;
        private readonly InvokeMethodOptions options;
        private readonly Action<GrainReference, IInvokable, InvokeMethodOptions> sendRequest;
        private readonly InterfaceToImplementationMappingCache mapping;
        private readonly IOutgoingGrainCallFilter[] filters;
        private readonly ResponseCompletionSource<TResult> responseCompletionSource;
        private readonly GrainReference grainReference;
        private int stage;
        private object[] _arguments;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutgoingCallInvoker{TResult}"/> class.
        /// </summary>
        /// <param name="grain">The grain reference.</param>
        /// <param name="request">The request.</param>
        /// <param name="options"></param>
        /// <param name="sendRequest"></param>
        /// <param name="filters">The invocation interceptors.</param>
        /// <param name="mapping">The implementation map.</param>
        /// <param name="responseCompletionSource">The object used to signal completion of the request.</param>
        public OutgoingCallInvoker(
            GrainReference grain,
            IInvokable request,
            InvokeMethodOptions options,
            Action<GrainReference, IInvokable, InvokeMethodOptions> sendRequest,
            InterfaceToImplementationMappingCache mapping,
            IOutgoingGrainCallFilter[] filters,
            ResponseCompletionSource<TResult> responseCompletionSource)
        {
            this.request = request;
            this.options = options;
            this.sendRequest = sendRequest;
            this.mapping = mapping;
            this.grainReference = grain;
            this.filters = filters;
            this.responseCompletionSource = responseCompletionSource;
        }

        /// <inheritdoc />
        public IAddressable Grain => this.grainReference;

        /// <inheritdoc />
        public MethodInfo Method
        {
            get
            {
                return null;
            }
        }

        /// <inheritdoc />
        public MethodInfo InterfaceMethod => this.Method;

        /// <inheritdoc />
        public object[] Arguments => _arguments ??= ExtractArguments();

        /// <inheritdoc />
        public object Result { get; set; }

        /// <inheritdoc />
        public async Task Invoke()
        {
            try
            {
                // Execute each stage in the pipeline. Each successive call to this method will invoke the next stage.
                // Stages which are not implemented (eg, because the user has not specified an interceptor) are skipped.
                var numFilters = filters.Length;
                if (stage < numFilters)
                {
                    // Call each of the specified interceptors.
                    var systemWideFilter = this.filters[stage];
                    stage++;
                    await systemWideFilter.Invoke(this);
                    return;
                }

                if (stage == numFilters)
                {
                    // Finally call the root-level invoker.
                    stage++;
                    var resultTask = this.sendRequest(this.grainReference, this.request, this.options);
                    if (resultTask != null)
                    {
                        this.Result = await responseCompletionSource.AsValueTask();
                    }

                    return;
                }
            }
            finally
            {
                stage--;
            }

            // If this method has been called more than the expected number of times, that is invalid.
            ThrowInvalidCall();
        }

        private static void ThrowInvalidCall()
        {
            throw new InvalidOperationException(
                $"{nameof(OutgoingCallInvoker)}.{nameof(Invoke)}() received an invalid call.");
        }

        private object[] ExtractArguments()
        {
            if (request.ArgumentCount == 0)
            {
                return Array.Empty<object>();
            }

            var result = new object[request.ArgumentCount];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = request.GetArgument<object>(i);
            }

            return result;
        }
    }
}