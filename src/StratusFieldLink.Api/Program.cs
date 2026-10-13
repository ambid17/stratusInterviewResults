using System.Text.Json;
using System.Text.Json.Serialization;
using StratusFieldLink.Api.Api;
using StratusFieldLink.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<ISketchRepository>(_ =>
    InMemorySketchRepository.FromSeedFile(SeedLocator.Locate()));

// The Unity player and the Vite dev server both call this from another origin.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

app.MapAuthEndpoints();
app.MapSketchEndpoints();
app.MapDashboardEndpoints();

var repo = app.Services.GetRequiredService<ISketchRepository>();
app.Logger.LogInformation("Seeded {Count} field sketches from {Path}", repo.All().Count, SeedLocator.Locate());

app.Run();

/// <summary>Exposed so integration tests can spin up the host.</summary>
public partial class Program
{
}
