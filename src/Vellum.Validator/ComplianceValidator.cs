using System.Text.RegularExpressions;
using Vellum.Extractor.Models;
using Vellum.Ingestor.Models;
using Vellum.Validator.Models;

namespace Vellum.Validator;

public record ValidationViolation(string Source, string Target, string Rule, string Description);

public class ComplianceValidator
{
    private readonly VellumConfig _config;

    public ComplianceValidator(VellumConfig? config = null)
    {
        _config = config ?? new VellumConfig();
    }

    public async Task<List<ValidationViolation>> ValidateAsync(DependencyGraph graph, DesignModel design)
    {
        var violations = new List<ValidationViolation>();
        var semanticMatcher = new SemanticMatcher(_config.Output.Format == "SARIF" ? null : null); // API key from env?

        // 1. Check if every design class exists in code (Semantic Alignment)
        foreach (var designClass in design.Classes)
        {
            bool found = false;
            foreach (var node in graph.Nodes)
            {
                if (await semanticMatcher.IsMatchAsync(node.Name, designClass.Name))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                violations.Add(new ValidationViolation(
                    designClass.Name,
                    "Code",
                    "DesignPresence",
                    $"Violation: Design class '{designClass.Name}' was not found in the source code (Architectural Decay)."
                ));
            }
        }

        // 2. Map namespaces to layers using config patterns
        string GetLayer(string ns)
        {
            foreach (var mapping in _config.NamespaceMapping)
            {
                var pattern = Regex.Escape(mapping.Pattern).Replace("\\*", ".*");
                if (Regex.IsMatch(ns, $"^{pattern}$", RegexOptions.IgnoreCase))
                {
                    return mapping.Layer;
                }
            }
            return "Unknown";
        }

        // 2. Filter exclusions
        bool IsExcluded(string typeId)
        {
            foreach (var exclusion in _config.Exclusions)
            {
                var pattern = Regex.Escape(exclusion).Replace("\\*", ".*");
                if (Regex.IsMatch(typeId, $"^{pattern}$", RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        // 3. Check each edge in the dependency graph
        foreach (var edge in graph.Edges)
        {
            if (IsExcluded(edge.SourceId) || IsExcluded(edge.TargetId)) continue;

            var sourceNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.SourceId);
            var targetNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.TargetId);

            if (sourceNode == null || targetNode == null) continue;

            var sourceLayer = GetLayer(sourceNode.Namespace);
            var targetLayer = GetLayer(targetNode.Namespace);

            if (sourceLayer == "Unknown" || targetLayer == "Unknown") continue;
            if (sourceLayer == targetLayer) continue;

            var rule = design.Rules.FirstOrDefault(r => r.SourceLayer == sourceLayer && r.TargetLayer == targetLayer);
            
            if (rule != null && !rule.IsAllowed)
            {
                violations.Add(new ValidationViolation(
                    sourceNode.Id,
                    targetNode.Id,
                    $"{sourceLayer} -x-> {targetLayer}",
                    $"Violation: {sourceNode.Name} in {sourceLayer} layer direct dependency on {targetNode.Name} in {targetLayer} layer."
                ));
            }
        }

        return violations;
    }

    public List<LayerMetrics> CalculateMetrics(DependencyGraph graph)
    {
        var metrics = new List<LayerMetrics>();
        var layers = _config.NamespaceMapping.Select(m => m.Layer).Distinct().ToList();

        // 1. Helper to get layer
        string GetLayer(string ns)
        {
            foreach (var mapping in _config.NamespaceMapping)
            {
                var pattern = Regex.Escape(mapping.Pattern).Replace("\\*", ".*");
                if (Regex.IsMatch(ns, $"^{pattern}$", RegexOptions.IgnoreCase))
                {
                    return mapping.Layer;
                }
            }
            return "Unknown";
        }

        foreach (var layer in layers)
        {
            var layerNodes = graph.Nodes.Where(n => GetLayer(n.Namespace) == layer).ToList();
            if (!layerNodes.Any()) continue;

            // Afferent Coupling (Ca): Incoming dependencies from other layers
            var ca = graph.Edges.Count(e => 
                GetLayer(graph.Nodes.FirstOrDefault(n => n.Id == e.TargetId)?.Namespace ?? "") == layer &&
                GetLayer(graph.Nodes.FirstOrDefault(n => n.Id == e.SourceId)?.Namespace ?? "") != layer);

            // Efferent Coupling (Ce): Outgoing dependencies to other layers
            var ce = graph.Edges.Count(e =>
                GetLayer(graph.Nodes.FirstOrDefault(n => n.Id == e.SourceId)?.Namespace ?? "") == layer &&
                GetLayer(graph.Nodes.FirstOrDefault(n => n.Id == e.TargetId)?.Namespace ?? "") != layer);

            double instability = (ca + ce == 0) ? 0 : (double)ce / (ca + ce);

            // Abstraction (A): Ratio of abstract types
            var totalTypes = layerNodes.Count;
            var abstractTypes = layerNodes.Count(n => n.Type == "interface" || n.Type == "abstract class");
            double abstraction = (double)abstractTypes / totalTypes;

            // Distance (D): |A + I - 1|
            double distance = Math.Abs(abstraction + instability - 1);

            metrics.Add(new LayerMetrics(layer, ca, ce, instability, abstraction, distance));
        }

        return metrics;
    }
}
