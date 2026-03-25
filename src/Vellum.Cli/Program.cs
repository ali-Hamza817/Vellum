using System.CommandLine;
using Vellum.Extractor;
using Vellum.Ingestor;
using Vellum.Validator;
using Vellum.Remediation;
using System.Text.Json;

namespace Vellum.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Vellum: Automated Design Enforcement for .NET");

        var checkCommand = new Command("check", "Check source code against architectural design");
        
        var solutionOption = new Option<string>(
            name: "--solution",
            description: "The path to the .NET solution file (.sln)") { IsRequired = true };
        
        var designOption = new Option<string>(
            name: "--design",
            description: "The path to the design file (PlantUML or Mermaid)") { IsRequired = true };

        var outputOption = new Option<string>(
            name: "--output",
            description: "The path to save the dependency graph (JSON)");

        checkCommand.AddOption(solutionOption);
        checkCommand.AddOption(designOption);
        checkCommand.AddOption(outputOption);

        checkCommand.SetHandler(async (solutionPath, designPath, outputPath) =>
        {
            Console.WriteLine($"Vellum: Analyzing solution {solutionPath}...");
            var extractor = new RoslynDependencyExtractor();
            var graph = await extractor.AnalyzeSolutionAsync(solutionPath);
            Console.WriteLine($"Extracted {graph.Nodes.Count} types and {graph.Edges.Count} dependencies.");

            Console.WriteLine($"Vellum: Ingesting design {designPath}...");
            var parser = new PlantUmlParser();
            var designContent = await File.ReadAllTextAsync(designPath);
            var design = parser.Parse(designContent);
            Console.WriteLine($"Ingested {design.Classes.Count} defined classes and {design.Rules.Count} architectural rules.");

            Console.WriteLine("Vellum: Validating compliance...");
            var validator = new ComplianceValidator();
            var violations = validator.Validate(graph, design);

            var remediation = new RemediationEngine();
            var suggestions = remediation.GenerateSuggestions(violations);

            if (violations.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] Found {violations.Count} architectural violations:");
                foreach (var v in violations)
                {
                    Console.WriteLine($"  - {v.Description} (Rule: {v.Rule})");
                    var s = suggestions.FirstOrDefault(su => su.ViolationSource == v.Source);
                    if (s != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"    [FIX] {s.Suggestion}");
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[PASS] No violations found.");
                Console.ResetColor();
            }

            if (!string.IsNullOrEmpty(outputPath))
            {
                var json = JsonSerializer.Serialize(new { Graph = graph, Design = design, Violations = violations, Remediation = suggestions }, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(outputPath, json);
                Console.WriteLine($"Report saved to {outputPath}");
            }

        }, solutionOption, designOption, outputOption);

        rootCommand.AddCommand(checkCommand);

        return await rootCommand.InvokeAsync(args);
    }
}
