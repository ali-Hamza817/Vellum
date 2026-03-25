using System.Text;
using Vellum.Extractor.Models;
using Vellum.Validator;

namespace Vellum.Cli.Reporting;

public class SvgVisualizer
{
    public static string GenerateDependencySvg(DependencyGraph graph, List<ValidationViolation> violations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1000\" height=\"800\">");
        sb.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"#f8f9fa\" />");
        sb.AppendLine("<text x=\"20\" y=\"30\" font-family=\"Arial\" font-size=\"20\" font-weight=\"bold\">Vellum: Architectural Dependency Map</text>");

        var nodePositions = new Dictionary<string, (int x, int y)>();
        int startX = 100, startY = 100, spacingX = 250, spacingY = 150;
        int currentX = startX, currentY = startY;

        // 1. Draw Nodes
        foreach (var node in graph.Nodes)
        {
            nodePositions[node.Id] = (currentX, currentY);
            
            sb.AppendLine($"<rect x=\"{currentX - 50}\" y=\"{currentY - 25}\" width=\"160\" height=\"50\" rx=\"5\" fill=\"#e9ecef\" stroke=\"#343a40\" stroke-width=\"1\" />");
            sb.AppendLine($"<text x=\"{currentX + 30}\" y=\"{currentY + 5}\" font-family=\"Arial\" font-size=\"12\" text-anchor=\"middle\">{node.Name}</text>");
            sb.AppendLine($"<text x=\"{currentX + 30}\" y=\"{currentY + 20}\" font-family=\"Arial\" font-size=\"10\" text-anchor=\"middle\" fill=\"#6c757d\">{node.Namespace}</text>");

            currentY += spacingY;
            if (currentY > 700) { currentY = startY; currentX += spacingX; }
        }

        // 2. Draw Edges
        foreach (var edge in graph.Edges)
        {
            if (nodePositions.TryGetValue(edge.SourceId, out var source) && nodePositions.TryGetValue(edge.TargetId, out var target))
            {
                var isViolation = violations.Any(v => v.Source == edge.SourceId && v.Target == edge.TargetId);
                var color = isViolation ? "#dc3545" : "#6c757d";
                var strokeWidth = isViolation ? "2" : "1";

                sb.AppendLine($"<line x1=\"{source.x + 110}\" y1=\"{source.y}\" x2=\"{target.x - 50}\" y2=\"{target.y}\" stroke=\"{color}\" stroke-width=\"{strokeWidth}\" marker-end=\"url(#arrowhead)\" />");
            }
        }

        // Marker definition
        sb.AppendLine("<defs><marker id=\"arrowhead\" markerWidth=\"10\" markerHeight=\"7\" refX=\"0\" refY=\"3.5\" orient=\"auto\">");
        sb.AppendLine("<polygon points=\"0 0, 10 3.5, 0 7\" fill=\"#6c757d\" /></marker></defs>");
        
        sb.AppendLine("</svg>");
        return sb.ToString();
    }
}
