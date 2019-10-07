using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.ApplicationParts;
using Orleans.CodeGenerator;
using Orleans.Metadata;
using Orleans.Serialization;

namespace Orleans.Hosting
{
    /// <summary>
    /// Extensions for <see cref="IApplicationPartManagerWithAssemblies"/> for invoking code generation.
    /// </summary>
    public static class ApplicationPartManagerCodeGenExtensions
    {
        /// <summary>
        /// Generates support code for the provided assembly and adds it to the builder.
        /// </summary>
        /// <param name="manager">The builder.</param>
        /// <param name="loggerFactory">The optional logger factory, for outputting code generation diagnostics.</param>
        /// <returns>A builder with support parts added.</returns>
        public static IApplicationPartManagerWithAssemblies WithCodeGeneration(this IApplicationPartManagerWithAssemblies manager, ILoggerFactory loggerFactory = null)
        {
            var stopWatch = Stopwatch.StartNew();
            loggerFactory = loggerFactory ?? new NullLoggerFactory();
            var tempPartManager = new ApplicationPartManager();
            foreach (var provider in manager.FeatureProviders)
            {
                tempPartManager.AddFeatureProvider(provider);
            }

            foreach (var part in manager.ApplicationParts)
            {
                tempPartManager.AddApplicationPart(part);
            }

            tempPartManager.AddApplicationPart(new AssemblyPart(typeof(IClientBuilder).Assembly));
            tempPartManager.AddFeatureProvider(new AssemblyAttributeFeatureProvider<GrainInterfaceFeature>());
            tempPartManager.AddFeatureProvider(new AssemblyAttributeFeatureProvider<GrainClassFeature>());
            tempPartManager.AddFeatureProvider(new AssemblyAttributeFeatureProvider<SerializerFeature>());
            tempPartManager.AddFeatureProvider(new Orleans.Serialization.Internal.BuiltInTypesSerializationFeaturePopulator());
            
            var codeGenerator = new Orleans.CodeGenerator.CodeGenerator(tempPartManager, loggerFactory);
            var generatedAssembly = codeGenerator.GenerateAndLoadForAssemblies(manager.Assemblies);
            stopWatch.Stop();
            var logger = loggerFactory.CreateLogger("RuntimeCodeGen");
            var assemblyNames = string.Join(", ", manager.Assemblies.Select(a => a.GetName().Name));
            logger?.LogInformation(0, $"Runtime code generation for assemblies {assemblyNames} took {stopWatch.ElapsedMilliseconds} milliseconds");
            return manager.AddApplicationPart(generatedAssembly);
        }
    }
}
