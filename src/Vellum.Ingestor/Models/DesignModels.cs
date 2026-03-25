namespace Vellum.Ingestor.Models;

public record DesignModel(List<DesignClass> Classes, List<LayerRule> Rules);

public record DesignClass(string Name, string? Layer = null);

public record LayerRule(string SourceLayer, string TargetLayer, bool IsAllowed);
