using System.Text.Json.Serialization;

namespace Vellum.Validator.Models;

public class VellumConfig
{
    [JsonPropertyName("NamespaceMapping")]
    public List<NamespaceMapping> NamespaceMapping { get; set; } = new();

    [JsonPropertyName("Exclusions")]
    public List<string> Exclusions { get; set; } = new();

    [JsonPropertyName("Metrics")]
    public MetricsConfig Metrics { get; set; } = new();

    [JsonPropertyName("Output")]
    public OutputConfig Output { get; set; } = new();
}

public class NamespaceMapping
{
    [JsonPropertyName("Pattern")]
    public string Pattern { get; set; } = string.Empty;

    [JsonPropertyName("Layer")]
    public string Layer { get; set; } = string.Empty;
}

public class MetricsConfig
{
    [JsonPropertyName("EnableMartinMetrics")]
    public bool EnableMartinMetrics { get; set; }
}

public class OutputConfig
{
    [JsonPropertyName("Format")]
    public string Format { get; set; } = "JSON";
}
