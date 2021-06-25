using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Orleans.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InheritFromGrainBaseAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ORLEANS0003";
        private const string BaseInterfaceName = "Orleans.IGrain";
        private const string BaseClassName = "Orleans.Grain";
        private const string BaseGrainReferenceName = "Orleans.Runtime.GrainReference";
        public const string Title = "Non-abstract classes that implement IGrain should derive from the base class Orleans.Grain";
        public const string MessageFormat = Title;
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var namedSymbol = context.Symbol as INamedTypeSymbol;

            // Continue if the class is not abstract.
            if (namedSymbol == null || namedSymbol.IsAbstract) return;

            // Continue only if there is no issue inside the class.
            var diagnostics = context.Compilation.GetDeclarationDiagnostics();
            if (diagnostics.Any()) return;

            // Continue only if the class implements IGrain
            var implementsGrainInterface = false;
            foreach (var iface in namedSymbol.AllInterfaces)
            {
                if (iface.MetadataName.Equals(BaseInterfaceName))
                {
                    implementsGrainInterface = true;
                }
            }

            if (!implementsGrainInterface)
            {
                return;
            }


            // Get the base type of the class
            var baseType = namedSymbol.BaseType;
            bool hasGrainBase = false;
            while (baseType is { })
            {
                if (baseType.MetadataName.Equals(BaseClassName) || baseType.MetadataName.Equals(BaseGrainReferenceName))
                {
                    hasGrainBase = true;
                    break;
                }

                baseType = baseType.BaseType;
            }

            if (!hasGrainBase)
            {
                var location = namedSymbol.Locations.FirstOrDefault();

                if (location != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, location));
                }
            }
        }
    }
}
