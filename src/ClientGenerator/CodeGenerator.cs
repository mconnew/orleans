using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Orleans.Logging;
using Orleans.Runtime.Configuration;
using Orleans.CodeGenerator;
using Orleans.Serialization;
using Orleans.Runtime;
using Orleans.Configuration;
#if NETCOREAPP2_0
using System.Runtime.Loader;
#endif

namespace Orleans.CodeGeneration
{
    public class CodeGenerator : MarshalByRefObject
    {
        public static readonly string OrleansAssemblyFileName = Path.GetFileName(typeof(RuntimeVersion).Assembly.Location);
        private static readonly int[] SuppressCompilerWarnings =
        {
            162, // CS0162 - Unreachable code detected.
            219, // CS0219 - The variable 'V' is assigned but its value is never used.
            414, // CS0414 - The private field 'F' is assigned but its value is never used.
            649, // CS0649 - Field 'F' is never assigned to, and will always have its default value.
            693, // CS0693 - Type parameter 'type parameter' has the same name as the type parameter from outer type 'T'
            1591, // CS1591 - Missing XML comment for publicly visible type or member 'Type_or_Member'
            1998 // CS1998 - This async method lacks 'await' operators and will run synchronously
        };
        
        public static bool GenerateCode(CodeGenOptions options)
        {
            var outputFileName = options.OutputFileName;

            // Create directory for output file if it does not exist
            var outputFileDirectory = Path.GetDirectoryName(outputFileName);

            if (!string.IsNullOrEmpty(outputFileDirectory) && !Directory.Exists(outputFileDirectory))
            {
                Directory.CreateDirectory(outputFileDirectory);
            }

            // Generate source
            Console.WriteLine($"Orleans-CodeGen - Generating file {outputFileName}");

#if !NETCOREAPP2_0
            var generatedCode = GenerateCodeInAppDomain(options);
#else
            var generatedCode = GenerateCodeInternal(options);
#endif
            
            using (var sourceWriter = new StreamWriter(outputFileName))
            {
                sourceWriter.WriteLine("#if !EXCLUDE_CODEGEN");
                DisableWarnings(sourceWriter, SuppressCompilerWarnings);
                sourceWriter.WriteLine(generatedCode ?? string.Empty);
                RestoreWarnings(sourceWriter, SuppressCompilerWarnings);
                sourceWriter.WriteLine("#endif");
            }

            Console.WriteLine($"Orleans-CodeGen - Generated file written {outputFileName}");
            return !string.IsNullOrWhiteSpace(generatedCode);
        }

        internal static string GenerateSourceForAssembly(Assembly grainAssembly)
        {
            using (var loggerFactory = new LoggerFactory())
            {
                var config = new ClusterConfiguration();
                loggerFactory.AddProvider(new ConsoleLoggerProvider(new ConsoleLoggerSettings()));
                var serializationProviderOptions = Options.Create(
                    new SerializationProviderOptions
                    {
                        SerializationProviders = config.Globals.SerializationProviders,
                        FallbackSerializationProvider = config.Globals.FallbackSerializationProvider
                    });
                var codeGenerator = new RoslynCodeGenerator(new SerializationManager(null, serializationProviderOptions, null, loggerFactory), loggerFactory);
                return codeGenerator.GenerateSourceForAssembly(grainAssembly);
            }
        }

        private static void SetupDepsFilesForAppDomain(AppDomain appDomain, FileInfo inputAssembly)
        {
            var thisAssemblyPath = new Uri(typeof(CodeGenerator).Assembly.CodeBase).LocalPath;
            // Specify the location of dependency context files.
            var codegenDepsFile = Path.Combine(Path.GetDirectoryName(thisAssemblyPath) ?? string.Empty, $"{Path.GetFileNameWithoutExtension(thisAssemblyPath)}.deps.json");
            var appDepsFile = Path.Combine(inputAssembly.DirectoryName, $"{Path.GetFileNameWithoutExtension(inputAssembly.Name)}.deps.json");
            var depsFiles = new List<string>();
            if (File.Exists(codegenDepsFile)) depsFiles.Add(codegenDepsFile);
            if (File.Exists(appDepsFile)) depsFiles.Add(appDepsFile);
            if (depsFiles.Count > 0)
            {
                appDomain.SetData("APP_CONTEXT_DEPS_FILES", string.Join(";", depsFiles));
            }
        }

#if !NETCOREAPP2_0
        private static string GenerateCodeInAppDomain(CodeGenOptions options)
        {
            AppDomain appDomain = null;
            try
            {
                var assembly = typeof(CodeGenerator).GetTypeInfo().Assembly;

                // Create AppDomain.
                var thisAssemblyPath = new Uri(assembly.CodeBase).LocalPath;
                var appDomainSetup = new AppDomainSetup
                {
                    ApplicationBase = Path.GetDirectoryName(thisAssemblyPath),
                    DisallowBindingRedirects = false,
                    ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
                };
                appDomain = AppDomain.CreateDomain("Orleans-CodeGen Domain", null, appDomainSetup);
                
                // Create an instance in the new app domain.
                var generator =
                    (CodeGenerator)
                    appDomain.CreateInstanceAndUnwrap(
                        assembly.FullName,
                        // ReSharper disable once AssignNullToNotNullAttribute
                        typeof(CodeGenerator).FullName);

                // Call the code generation method.
                return generator.GenerateCodeInCurrentAppDomain(options);
            }
            finally
            {
                if (appDomain != null) AppDomain.Unload(appDomain); // Unload the AppDomain
            }
        }

        private string GenerateCodeInCurrentAppDomain(CodeGenOptions options)
        {
            return GenerateCodeInternal(options);
        }
#endif

        private static string GenerateCodeInternal(CodeGenOptions options)
        {
            SetupDepsFilesForAppDomain(AppDomain.CurrentDomain, options.InputAssembly);

            var inputAssembly = options.InputAssembly.FullName;
            var referencedAssemblies = options.ReferencedAssemblies;

            // Set up assembly resolver
            var refResolver = new AssemblyResolver(inputAssembly, referencedAssemblies, Console.WriteLine);

            try
            {
                // Set up assembly resolution.
                AppDomain.CurrentDomain.AssemblyResolve += refResolver.ResolveAssembly;
#if NETCOREAPP2_0
                AssemblyLoadContext.Default.Resolving += refResolver.AssemblyLoadContextResolving;
#endif

                return GenerateSourceForAssembly(refResolver.Assembly);
            }
            finally
            {
                refResolver.Dispose();
                AppDomain.CurrentDomain.AssemblyResolve -= refResolver.ResolveAssembly;
#if NETCOREAPP2_0
                AssemblyLoadContext.Default.Resolving -= refResolver.AssemblyLoadContextResolving;
#endif
            }
        }

        private static void DisableWarnings(TextWriter sourceWriter, IEnumerable<int> warnings)
        {
            foreach (var warningNum in warnings) sourceWriter.WriteLine("#pragma warning disable {0}", warningNum);
        }

        private static void RestoreWarnings(TextWriter sourceWriter, IEnumerable<int> warnings)
        {
            foreach (var warningNum in warnings) sourceWriter.WriteLine("#pragma warning restore {0}", warningNum);
        }
    }
}