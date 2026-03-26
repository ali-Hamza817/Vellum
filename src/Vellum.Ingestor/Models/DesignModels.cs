namespace Vellum.Ingestor.Models;

public record DesignModel(List<DesignClass> Classes, List<LayerRule> Rules)
{
    public DesignModel() : this(new List<DesignClass>(), new List<LayerRule>()) { }
}

public record DesignClass(string Name, string? Layer = null);

public record LayerRule(string SourceLayer, string TargetLayer, bool IsAllowed);
