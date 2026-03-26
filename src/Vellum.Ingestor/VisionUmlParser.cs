using System.Net.Http.Json;
using System.Text.Json;
using Vellum.Ingestor.Models;

namespace Vellum.Ingestor;

public class VisionUmlParser
{
    private readonly string? _apiKey;

    public VisionUmlParser(string? apiKey = null)
    {
        _apiKey = apiKey;
    }

    public async Task<DesignModel> ParseImageAsync(string imagePath)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            throw new InvalidOperationException("Vision API key (OpenAI/Gemini) required for image-based UML interpretation.");
        }

        byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
        string base64Image = Convert.ToBase64String(imageBytes);

        return await CallVisionLlmAsync(base64Image);
    }

    private async Task<DesignModel> CallVisionLlmAsync(string base64Image)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var requestBody = new
        {
            model = "gpt-4o-mini",
            messages = new object[]
            {
                new 
                { 
                    role = "system", 
                    content = "You are an expert software architect. Analyze the provided UML diagram image and extract the architectural structure in a JSON format. " +
                              "Return a JSON object with: 1) 'classes' (array of strings for class names) and 2) 'rules' (array of objects with 'source', 'target', and 'isAllowed' boolean). " +
                              "For lines with a cross (X) or labeled as 'forbidden', set 'isAllowed' to false."
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = "Extract the architecture from this diagram." },
                        new { type = "image_url", image_url = new { url = $"data:image/png;base64,{base64Image}" } }
                    }
                }
            },
            response_format = new { type = "json_object" },
            max_tokens = 1000
        };

        var response = await client.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var content = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        if (string.IsNullOrEmpty(content)) return new DesignModel();

        var parsed = JsonSerializer.Deserialize<VisionResult>(content);
        
        var model = new DesignModel();
        if (parsed?.Classes != null)
        {
            foreach (var cls in parsed.Classes) model.Classes.Add(new DesignClass(cls));
        }

        if (parsed?.Rules != null)
        {
            foreach (var rule in parsed.Rules)
            {
                model.Rules.Add(new LayerRule(rule.Source, rule.Target, rule.IsAllowed));
            }
        }

        return model;
    }

    private class VisionResult
    {
        public List<string> Classes { get; set; } = new();
        public List<VisionRule> Rules { get; set; } = new();
    }

    private class VisionRule
    {
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public bool IsAllowed { get; set; }
    }
}
