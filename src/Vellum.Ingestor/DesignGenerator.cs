using System.Text;
using Vellum.Extractor.Models;

namespace Vellum.Ingestor;

public class DesignGenerator
{
    public string GeneratePlantUml(DependencyGraph graph)
    {
        var sb = new StringBuilder();
        sb.AppendLine("@startuml");
        sb.AppendLine("skinparam packageStyle rectangle");
        sb.AppendLine("hide empty members");

        // 1. Group nodes by namespace for hierarchical visualization
        var groups = graph.Nodes.GroupBy(n => n.Namespace);

        foreach (var group in groups)
        {
            sb.AppendLine($"package \"{group.Key}\" {{");
            foreach (var node in group)
            {
                string typePrefix = node.Type.ToLower().Contains("interface") ? "interface" : "class";
                sb.AppendLine($"    {typePrefix} {node.Name}");
            }
            sb.AppendLine("}");
        }

        // 2. Map edges
        foreach (var edge in graph.Edges)
        {
            var source = graph.Nodes.FirstOrDefault(n => n.Id == edge.SourceId);
            var target = graph.Nodes.FirstOrDefault(n => n.Id == edge.TargetId);

            if (source != null && target != null)
            {
                sb.AppendLine($"{source.Name} ..> {target.Name}");
            }
        }

        sb.AppendLine("@enduml");
        return sb.ToString();
    }

    public string GenerateMermaid(DependencyGraph graph)
    {
        var sb = new StringBuilder();
        sb.AppendLine("classDiagram");

        var groups = graph.Nodes.GroupBy(n => n.Namespace);
        int groupIndex = 0;

        foreach (var group in groups)
        {
            sb.AppendLine($"    subgraph {group.Key.Replace(".", "_")}");
            foreach (var node in group)
            {
                sb.AppendLine($"        class {node.Name}");
            }
            sb.AppendLine("    end");
            groupIndex++;
        }

        foreach (var edge in graph.Edges)
        {
            var source = graph.Nodes.FirstOrDefault(n => n.Id == edge.SourceId);
            var target = graph.Nodes.FirstOrDefault(n => n.Id == edge.TargetId);

            if (source != null && target != null)
            {
                sb.AppendLine($"    {source.Name} ..> {target.Name}");
            }
        }

        return sb.ToString();
    }
}
