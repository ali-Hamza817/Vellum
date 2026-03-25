using System.Net.Http.Json;
using System.Text.Json;
using Vellum.Validator.Models;

namespace Vellum.Validator;

public class SemanticMatcher
{
    private readonly string? _apiKey;

    public SemanticMatcher(string? apiKey = null)
    {
        _apiKey = apiKey;
    }

    public async Task<bool> IsMatchAsync(string codeName, string designName)
    {
        // 1. Exact match
        if (string.Equals(codeName, designName, StringComparison.OrdinalIgnoreCase)) return true;

        // 2. Simple heuristic (substring)
        if (codeName.Contains(designName, StringComparison.OrdinalIgnoreCase) || 
            designName.Contains(codeName, StringComparison.OrdinalIgnoreCase)) return true;

        // 3. LLM-powered semantic match (State-of-the-Art)
        if (!string.IsNullOrEmpty(_apiKey))
        {
            return await CallLlmToVerifyMatch(codeName, designName);
        }

        return false;
    }

    private async Task<bool> CallLlmToVerifyMatch(string codeName, string designName)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = "You are an architectural assistant. Return 'YES' or 'NO' if two classes are semantically equivalent in a software system." },
                    new { role = "user", content = $"Is the class '{codeName}' semantically equivalent or a valid implementation of the design entity '{designName}'?" }
                },
                max_tokens = 5
            };

            var response = await client.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                var content = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()?.Trim().ToUpper();
                return content == "YES";
            }
        }
        catch
        {
            // Fallback for connectivity issues
        }

        return false;
    }
}
