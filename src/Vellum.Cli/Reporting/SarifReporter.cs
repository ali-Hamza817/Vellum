using Vellum.Validator;

namespace Vellum.Cli.Reporting;

public class SarifReporter
{
    public static string GenerateSarif(List<ValidationViolation> violations)
    {
        var sarif = new
        {
            version = "2.1.0",
            @schema = "https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0-rtm.5.json",
            runs = new[]
            {
                new
                {
                    tool = new
                    {
                        driver = new
                        {
                            name = "Vellum",
                            version = "2.0.0",
                            rules = new[]
                            {
                                new
                                {
                                    id = "VLM001",
                                    name = "ArchitecturalLayerViolation",
                                    shortDescription = new { text = "Architectural Layer Violation" },
                                    fullDescription = new { text = "A type depends on another type in a forbidden architectural layer." }
                                }
                            }
                        }
                    },
                    results = violations.Select(v => new
                    {
                        ruleId = "VLM001",
                        message = new { text = v.Description },
                        locations = new[]
                        {
                            new
                            {
                                physicalLocation = new
                                {
                                    artifactLocation = new { uri = v.Source }, // Source ID is the type full name
                                    // In a full implementation, we'd include line numbers from Roslyn
                                }
                            }
                        }
                    }).ToArray()
                }
            }
        };

        return System.Text.Json.JsonSerializer.Serialize(sarif, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
