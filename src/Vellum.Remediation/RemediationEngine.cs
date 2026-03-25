using Vellum.Validator;

namespace Vellum.Remediation;

public record DesignFixSuggestion(string ViolationSource, string Suggestion, string Description);

public class RemediationEngine
{
    public List<DesignFixSuggestion> GenerateSuggestions(List<ValidationViolation> violations)
    {
        var suggestions = new List<DesignFixSuggestion>();

        foreach (var v in violations)
        {
            if (v.Rule.Contains("-x->"))
            {
                // Simple rule-based remediation for layer violations
                var components = v.Rule.Split(" -x-> ");
                var sourceLayer = components[0];
                var targetLayer = components[1];

                string suggestion = $"Move the dependency to an intermediate layer (e.g., Service) or extract an interface.";
                string description = $"The type '{v.Source}' in layer '{sourceLayer}' should not directly reference '{v.Target}' in layer '{targetLayer}'. " +
                                     $"Consider introducing a service in the '{sourceLayer}' layer or an abstraction in a shared layer.";

                suggestions.Add(new DesignFixSuggestion(v.Source, suggestion, description));
            }
        }

        return suggestions;
    }

    public async Task<string> GetAISuggestionAsync(ValidationViolation violation)
    {
        // Stub for LLM integration as discussed in the implementation plan
        // This could call OpenAI or a local LLM in the future.
        return await Task.FromResult("AI Suggestion: Re-engineer the coupling between these components to follow the Dependency Inversion Principle.");
    }
}
