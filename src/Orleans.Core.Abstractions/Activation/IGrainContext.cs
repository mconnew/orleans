
using System;
using System.Collections.Generic;

namespace Orleans.Runtime
{
    /// <summary>
    /// Represents a grain from the perspective of the runtime.
    /// </summary>
    public interface IGrainContext : IEquatable<IGrainContext>
    {
        GrainReference GrainReference { get; }

        GrainId GrainId { get; }

        /// <summary>Gets the instance of the grain associated with this activation context. 
        /// The value will be <see langword="null"/> if the grain is being created.</summary>
        IAddressable GrainInstance { get; }

        ActivationId ActivationId { get; }

        ActivationAddress Address { get; }

        /// <summary>Gets the <see cref="IServiceProvider"/> that provides access to the grain activation's service container.</summary>
        IServiceProvider ActivationServices { get; }

        /// <summary>Gets a key/value collection that can be used to share data within the scope of the grain activation.</summary>
        IDictionary<object, object> Items { get; }

        /// <summary>
        /// Observable Grain life cycle
        /// </summary>
        IGrainLifecycle ObservableLifecycle { get; }

    }

    internal interface IActivationData : IGrainContext
    {
        IGrainRuntime Runtime { get; }

        void DelayDeactivation(TimeSpan timeSpan);
        void OnTimerCreated(IGrainTimer timer);
        void OnTimerDisposed(IGrainTimer timer);
    }

    public interface IGrainContextAccessor
    {
        IGrainContext GrainContext { get; }
    }
}
