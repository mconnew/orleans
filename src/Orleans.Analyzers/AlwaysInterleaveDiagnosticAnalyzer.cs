using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Orleans.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AlwaysInterleaveDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private const string AlwaysInterleaveAttributeName = "Orleans.Concurrency.AlwaysInterleaveAttribute";

        public const string DiagnosticId = "ORLEANS0001";
        public const string Title = "[AlwaysInterleave] must only be used on the grain interface method and not the grain class method.";
        public const string MessageFormat = Title;
        public const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(
                AnalyzeSymbol,
                SymbolKind.Method);
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            if (context.Symbol.ContainingSymbol is not INamedTypeSymbol containing || containing.TypeKind != TypeKind.Class)
            {
                return;
            }

            foreach (var attr in context.Symbol.GetAttributes())
            {
                if (attr.AttributeClass is { } attrType && attrType.MetadataName.Equals(AlwaysInterleaveAttributeName))
                {
                    var syntaxReference = attr.ApplicationSyntaxReference;
                    context.ReportDiagnostic(
                        Diagnostic.Create(Rule, Location.Create(syntaxReference.SyntaxTree, syntaxReference.Span)));
                }
            }
        }
    }
}

