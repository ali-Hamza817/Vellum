using Vellum.Extractor.Models;
using Vellum.Ingestor.Models;

namespace Vellum.Validator;

public record ValidationViolation(string Source, string Target, string Rule, string Description);

public class ComplianceValidator
{
    public List<ValidationViolation> Validate(DependencyGraph graph, DesignModel design)
    {
        var violations = new List<ValidationViolation>();

        // 1. Map namespaces to layers (Simplified for PoC)
        // In a real version, this would be configurable or inferred from the design
        string GetLayer(string ns)
        {
            if (ns.Contains(".UI", StringComparison.OrdinalIgnoreCase)) return "UI";
            if (ns.Contains(".Service", StringComparison.OrdinalIgnoreCase)) return "Service";
            if (ns.Contains(".Data", StringComparison.OrdinalIgnoreCase)) return "Data";
            return "Unknown";
        }

        // 2. Check each edge in the dependency graph
        foreach (var edge in graph.Edges)
        {
            var sourceNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.SourceId);
            var targetNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.TargetId);

            if (sourceNode == null || targetNode == null) continue;

            var sourceLayer = GetLayer(sourceNode.Namespace);
            var targetLayer = GetLayer(targetNode.Namespace);

            if (sourceLayer == "Unknown" || targetLayer == "Unknown") continue;
            if (sourceLayer == targetLayer) continue;

            // Find if there is a rule for this layer pair
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
            else if (rule == null)
            {
                // Strict mode: if no rule exists between layers, it might be a violation
                // For PoC, we only check explicit 'x' rules from the design
            }
        }

        return violations;
    }
}
