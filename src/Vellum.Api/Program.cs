using Vellum.Extractor;
using Vellum.Ingestor;
using Vellum.Ingestor.Models;
using Vellum.Validator;
using Vellum.Validator.Models;
using Vellum.Remediation;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors();

if (app.Environment.IsDevelopment())
{
}

app.MapPost("/check", async ([FromBody] CheckRequest request) =>
{
    if (!File.Exists(request.SolutionPath)) return Results.BadRequest($"Solution file not found: {request.SolutionPath}");
    if (!File.Exists(request.DesignPath)) return Results.BadRequest($"Design file not found: {request.DesignPath}");

    VellumConfig? config = null;
    var configPath = request.ConfigPath ?? "vellum.json";
    if (File.Exists(configPath))
    {
        var configContent = await File.ReadAllTextAsync(configPath);
        config = JsonSerializer.Deserialize<VellumConfig>(configContent);
    }

    var extractor = new RoslynDependencyExtractor();
    var graph = await extractor.AnalyzeSolutionAsync(request.SolutionPath);

    DesignModel design;
    var extension = Path.GetExtension(request.DesignPath).ToLower();
    if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
    {
        var apiKey = Environment.GetEnvironmentVariable("VELLUM_API_KEY");
        var visionParser = new VisionUmlParser(apiKey);
        design = await visionParser.ParseImageAsync(request.DesignPath);
    }
    else
    {
        var parser = new PlantUmlParser();
        var designContent = await File.ReadAllTextAsync(request.DesignPath);
        design = parser.Parse(designContent);
    }

    var validator = new ComplianceValidator(config);
    var violations = await validator.ValidateAsync(graph, design);
    var metrics = validator.CalculateMetrics(graph);
    var remediation = new RemediationEngine();
    var suggestions = remediation.GenerateSuggestions(violations);

    return Results.Ok(new { violations, metrics, suggestions });
})
.WithName("CheckArchitecture");

app.Run();

public record CheckRequest(string SolutionPath, string DesignPath, string? ConfigPath);
