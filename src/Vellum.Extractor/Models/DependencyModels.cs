namespace Vellum.Extractor.Models;

public record DependencyGraph(List<Node> Nodes, List<Edge> Edges);

public record Node(string Id, string Name, string Type, string Namespace);

public record Edge(string SourceId, string TargetId, string RelationType);
