using Microsoft.Extensions.DependencyInjection;
using Orleans.ApplicationParts;
using Orleans.Hosting;
using HostContext = Microsoft.Extensions.Hosting.HostBuilderContext;

namespace Orleans.Hosting
{
    public static class RuntimeCodeGenerationHostingExtensions
    {
        private static readonly object RuntimeCodeGenerationAddedKey = new object();

        public static ISiloBuilder AddRuntimeCodeGeneration(ISiloBuilder siloBuilder)
        {
            return siloBuilder.ConfigureServices((context, services) =>
            {
                if (!context.Properties.TryGetValue(RuntimeCodeGenerationAddedKey, out _))
                {
                    siloBuilder.ConfigureApplicationParts(parts => RuntimeCodeGeneratorUtility.GenerateCode(context, services, parts));
                    context.Properties[RuntimeCodeGenerationAddedKey] = RuntimeCodeGenerationAddedKey;
                }
            });
        }
    }

    internal static class RuntimeCodeGeneratorUtility
    {
        public static void GenerateCode(HostContext hostBuilderContext, IServiceCollection services, IApplicationPartManager applicationPartManager)
        {

        }
    }
}
