using System.CommandLine;
using Vellum.Extractor;
using Vellum.Ingestor;
using Vellum.Validator;
using Vellum.Validator.Models;
using Vellum.Remediation;
using Vellum.Cli.Reporting;
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

        var configOption = new Option<string>(
            name: "--config",
            description: "The path to the vellum.json configuration file");

        var outputOption = new Option<string>(
            name: "--output",
            description: "The path to save the report");

        var formatOption = new Option<string>(
            name: "--format",
            description: "The report format (JSON, SARIF, SVG)") { Arity = ArgumentArity.ZeroOrOne };
        formatOption.SetDefaultValue("JSON");

        checkCommand.AddOption(solutionOption);
        checkCommand.AddOption(designOption);
        checkCommand.AddOption(configOption);
        checkCommand.AddOption(outputOption);
        checkCommand.AddOption(formatOption);

        checkCommand.SetHandler(async (solutionPath, designPath, configPath, outputPath, format) =>
        {
            VellumConfig? config = null;
            if (File.Exists(configPath ?? "vellum.json"))
            {
                var configContent = await File.ReadAllTextAsync(configPath ?? "vellum.json");
                config = JsonSerializer.Deserialize<VellumConfig>(configContent);
                Console.WriteLine($"Vellum: Loaded configuration from {configPath ?? "vellum.json"}.");
            }

            Console.WriteLine($"Vellum: Analyzing solution {solutionPath}...");
            var extractor = new RoslynDependencyExtractor();
            var graph = await extractor.AnalyzeSolutionAsync(solutionPath);
            Console.WriteLine($"Extracted {graph.Nodes.Count} types and {graph.Edges.Count} dependencies.");

            Console.WriteLine($"Vellum: Ingesting design {designPath}...");
            DesignModel design;
            var extension = Path.GetExtension(designPath).ToLower();

            if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
            {
                var apiKey = Environment.GetEnvironmentVariable("VELLUM_API_KEY");
                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Warning: VELLUM_API_KEY not found. Vision parsing might fail.");
                    Console.ResetColor();
                }
                var visionParser = new VisionUmlParser(apiKey);
                design = await visionParser.ParseImageAsync(designPath);
            }
            else
            {
                var parser = new PlantUmlParser();
                var designContent = await File.ReadAllTextAsync(designPath);
                design = parser.Parse(designContent);
            }
            
            Console.WriteLine($"Ingested {design.Classes.Count} defined classes and {design.Rules.Count} architectural rules.");

            Console.WriteLine("Vellum: Validating compliance...");
            var validator = new ComplianceValidator(config);
            var violations = await validator.ValidateAsync(graph, design);

            var metrics = validator.CalculateMetrics(graph);

            var remediation = new RemediationEngine();
            var suggestions = remediation.GenerateSuggestions(violations);

            if (metrics.Count > 0)
            {
                Console.WriteLine("\nVellum: Architectural Metrics");
                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.WriteLine("{0,-15} | {1,-10} | {2,-10} | {3,-12} | {4,-12} | {5,-10}", "Layer", "Coupling", "Instabil.", "Abstraction", "Distance", "Health");
                Console.WriteLine("--------------------------------------------------------------------------------");
                foreach (var m in metrics)
                {
                    string health = m.Distance < 0.2 ? "Excellent" : m.Distance < 0.5 ? "Degrading" : "Critical";
                    Console.WriteLine("{0,-15} | {1,2}a/{2,2}e   | {3,-10:F2} | {4,-12:F2} | {5,-10:F2} | {6,-10}", 
                        m.Layer, m.AfferentCoupling, m.EfferentCoupling, m.Instability, m.Abstraction, m.Distance, health);
                }
                Console.WriteLine("--------------------------------------------------------------------------------");
            }

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
                string content;
                if (format?.ToUpper() == "SARIF")
                {
                    content = SarifReporter.GenerateSarif(violations);
                }
                else if (format?.ToUpper() == "SVG")
                {
                    content = SvgVisualizer.GenerateDependencySvg(graph, violations);
                }
                else
                {
                    content = JsonSerializer.Serialize(new { Graph = graph, Design = design, Violations = violations, Remediation = suggestions }, new JsonSerializerOptions { WriteIndented = true });
                }
                await File.WriteAllTextAsync(outputPath, content);
                Console.WriteLine($"{format} report saved to {outputPath}");
            }

        }, solutionOption, designOption, configOption, outputOption, formatOption);

        rootCommand.AddCommand(checkCommand);

        return await rootCommand.InvokeAsync(args);
    }
}
