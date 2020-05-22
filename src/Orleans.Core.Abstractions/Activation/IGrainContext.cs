
using System;

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

        /// <summary>
        /// Observable Grain life cycle
        /// </summary>
        IGrainLifecycle ObservableLifecycle { get; }

        void SetComponent<TComponent>(TComponent value);
            
        /// <summary>
        /// Gets the component of the specified type.
        /// </summary>
        TComponent GetComponent<TComponent>();
    }

    internal interface IActivationData : IGrainContext
    {
        IGrainRuntime Runtime { get; }

        void DelayDeactivation(TimeSpan timeSpan);
    }

    public interface IGrainContextAccessor
    {
        IGrainContext GrainContext { get; }
    }

    public interface IGrainExtensionBinder
    {
        TExtensionInterface GetExtension<TExtensionInterface>() where TExtensionInterface : IGrainExtension;

        /// <summary>
        /// Binds an extension to an addressable object, if not already done.
        /// </summary>
        /// <typeparam name="TExtension">The type of the extension (e.g. StreamConsumerExtension).</typeparam>
        /// <typeparam name="TExtensionInterface">The public interface type of the implementation.</typeparam>
        /// <param name="newExtensionFunc">A factory function that constructs a new extension object.</param>
        /// <returns>A tuple, containing first the extension and second an addressable reference to the extension's interface.</returns>
        (TExtension, TExtensionInterface) GetOrSetExtension<TExtension, TExtensionInterface>(Func<TExtension> newExtensionFunc)
            where TExtension : TExtensionInterface
            where TExtensionInterface : IGrainExtension;
    }
}
