using System;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Providers
{
    /// <summary>
    /// Interface to allow callbacks from providers into their assigned provider-manager.
    /// This allows access to runtime functionality, such as logging.
    /// </summary>
    /// <remarks>
    /// Passed to the provider during IProvider.Init call to that provider instance.
    /// </remarks>
    /// <seealso cref="IProvider"/>
    public interface IProviderRuntime
    {
        /// <summary>
        /// Factory for getting references to grains.
        /// </summary>
        IGrainFactory GrainFactory { get; }

        /// <summary>
        /// Service provider for dependency injection
        /// </summary>
        IServiceProvider ServiceProvider { get; }
    }
}
