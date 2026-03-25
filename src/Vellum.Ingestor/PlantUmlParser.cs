using System.Text.RegularExpressions;
using Vellum.Ingestor.Models;

namespace Vellum.Ingestor;

public class PlantUmlParser
{
    public DesignModel Parse(string content)
    {
        var classes = new List<DesignClass>();
        var rules = new List<LayerRule>();

        // This is a naive regex parser for PoC
        // Example: class OrderController <<UI>>
        var classRegex = new Regex(@"class\s+(?<name>\w+)(\s+<<(?<layer>\w+)>>)?", RegexOptions.IgnoreCase);
        foreach (Match match in classRegex.Matches(content))
        {
            classes.Add(new DesignClass(match.Groups["name"].Value, match.Groups["layer"].Value));
        }

        // Example: [UI] -> [Service] or [UI] -x-> [Data]
        var ruleRegex = new Regex(@"\[(?<source>\w+)\]\s+(?<arrow>[-x>]+)\s+\[(?<target>\w+)\]", RegexOptions.IgnoreCase);
        foreach (Match match in ruleRegex.Matches(content))
        {
            var arrow = match.Groups["arrow"].Value;
            var isAllowed = !arrow.Contains('x');
            rules.Add(new LayerRule(match.Groups["source"].Value, match.Groups["target"].Value, isAllowed));
        }

        return new DesignModel(classes, rules);
    }
}
