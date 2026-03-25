using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Vellum.Extractor.Models;

namespace Vellum.Extractor;

public class RoslynDependencyExtractor
{
    static RoslynDependencyExtractor()
    {
        MSBuildLocator.RegisterDefaults();
    }

    public async Task<DependencyGraph> AnalyzeSolutionAsync(string solutionPath)
    {
        using var workspace = MSBuildWorkspace.Create();
        
        workspace.WorkspaceFailed += (sender, e) => 
        {
            Console.WriteLine($"[Workspace Warning] {e.Diagnostic.Kind}: {e.Diagnostic.Message}");
        };

        var solution = await workspace.OpenSolutionAsync(solutionPath);
        
        foreach (var diag in workspace.Diagnostics)
        {
            Console.WriteLine($"[Workspace Diag] {diag.Kind}: {diag.Message}");
        }
        var nodes = new List<Node>();
        var edges = new List<Edge>();
        var processedTypes = new HashSet<string>();

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = await syntaxTree.GetRootAsync();

                // Find all type declarations (classes, interfaces, records)
                var typeDeclarations = root.DescendantNodes().OfType<TypeDeclarationSyntax>();
                foreach (var typeDecl in typeDeclarations)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(typeDecl);
                    if (symbol == null) continue;

                    var typeId = symbol.ToDisplayString();
                    if (processedTypes.Add(typeId))
                    {
                        nodes.Add(new Node(
                            typeId,
                            symbol.Name,
                            typeDecl.Keyword.ValueText,
                            symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty
                        ));
                    }

                    // Extract dependencies from the type's members (method signatures, field types, etc.)
                    // This is a simplified version of what was requested
                    var typeReferences = typeDecl.DescendantNodes().OfType<TypeSyntax>();
                    foreach (var typeRef in typeReferences)
                    {
                        var targetSymbol = semanticModel.GetSymbolInfo(typeRef).Symbol;
                        if (targetSymbol != null && targetSymbol.Kind == SymbolKind.NamedType)
                        {
                            var targetId = targetSymbol.ToDisplayString();
                            if (targetId != typeId) // Avoid self-references
                            {
                                edges.Add(new Edge(typeId, targetId, "Reference"));
                            }
                        }
                    }
                }
            }
        }

        return new DependencyGraph(nodes, edges.Distinct().ToList());
    }
}
