using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace StratusFieldLink.Api.Tests;

/// <summary>
/// Drives POST /api/sketches through the real host, seeded the same way the API is.
/// </summary>
public class PublishSketchEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PublishSketchEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private static string NewId() => "SK-T" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

    /// <summary>Shaped the way Unity's JsonUtility writes a FieldSketchDto.</summary>
    private static string Body(
        string id,
        string discipline = "Piping",
        string nodes = "[{\"x\":0.0,\"y\":10.0,\"z\":0.0},{\"x\":24.0,\"y\":10.0,\"z\":0.0}]",
        string extra = "")
        => "{\"id\":\"" + id + "\",\"packageId\":\"PKG-CHW-L02\",\"crewId\":\"CREW-01\"," +
           "\"discipline\":\"" + discipline + "\",\"diameterInches\":4.0,\"material\":\"Carbon Steel\"," +
           "\"nodes\":" + nodes + ",\"capturedAtUtc\":\"2026-10-13T17:00:00.0000000Z\"," +
           "\"clientLengthFeet\":24.0,\"clientVersion\":\"field-1.4.2\"" + extra + "}";

    private Task<HttpResponseMessage> Post(string json)
        => _client.PostAsync("/api/sketches", new StringContent(json, Encoding.UTF8, "application/json"));

    private static async Task<JsonElement> Json(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<JsonElement> Get(string path)
        => await _client.GetFromJsonAsync<JsonElement>(path);

    [Fact]
    public async Task Publish_NewSketch_Returns201AndIsVisibleWithoutRestart()
    {
        var id = NewId();
        var totalBefore = (await Get("/api/dashboard")).GetProperty("totalSketches").GetInt32();

        var response = await Post(Body(id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/sketches/{id}", response.Headers.Location?.OriginalString);

        var summary = (await Json(response)).GetProperty("summary");
        Assert.Equal(id, summary.GetProperty("id").GetString());
        Assert.Equal("Published", summary.GetProperty("status").GetString());
        Assert.Equal(24, summary.GetProperty("lengthFeet").GetDouble());
        Assert.NotEqual(JsonValueKind.Null, summary.GetProperty("publishedAtUtc").ValueKind);

        var list = await Get("/api/sketches?crewId=CREW-01&limit=500");
        Assert.Contains(list.EnumerateArray(), s => s.GetProperty("id").GetString() == id);

        var detail = await Get($"/api/sketches/{id}");
        var history = Assert.Single(detail.GetProperty("history").EnumerateArray());
        Assert.Equal("Draft", history.GetProperty("from").GetString());
        Assert.Equal("Published", history.GetProperty("to").GetString());
        Assert.Equal("CREW-01", history.GetProperty("actor").GetString());

        var totalAfter = (await Get("/api/dashboard")).GetProperty("totalSketches").GetInt32();
        Assert.Equal(totalBefore + 1, totalAfter);
    }

    [Fact]
    public async Task Publish_SameSketchTwice_IsIdempotent()
    {
        var id = NewId();

        var first = await Post(Body(id));
        var second = await Post(Body(id));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var firstPublishedAt = (await Json(first)).GetProperty("summary").GetProperty("publishedAtUtc").GetString();
        var detail = await Json(second);
        Assert.Equal(firstPublishedAt, detail.GetProperty("summary").GetProperty("publishedAtUtc").GetString());
        Assert.Single(detail.GetProperty("history").EnumerateArray());

        var list = await Get("/api/sketches?crewId=CREW-01&limit=500");
        Assert.Single(list.EnumerateArray(), s => s.GetProperty("id").GetString() == id);
    }

    [Fact]
    public async Task Publish_RetryFromNewerClient_IsStillARetry()
    {
        var id = NewId();
        await Post(Body(id));

        var retry = await Post(Body(id).Replace("field-1.4.2", "field-1.5.0").Replace("\"clientLengthFeet\":24.0", "\"clientLengthFeet\":23.5"));

        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
    }

    [Fact]
    public async Task Publish_SameIdDifferentRun_Returns409AndKeepsTheOriginal()
    {
        var id = NewId();
        await Post(Body(id));

        var conflict = await Post(Body(id, nodes: "[{\"x\":0.0,\"y\":10.0,\"z\":0.0},{\"x\":40.0,\"y\":10.0,\"z\":0.0}]"));

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace((await Json(conflict)).GetProperty("error").GetString()));

        var stored = await Get($"/api/sketches/{id}");
        Assert.Equal(24, stored.GetProperty("summary").GetProperty("lengthFeet").GetDouble());
    }

    [Fact]
    public async Task Publish_OverRejectedSketch_RepublishesAfterRework()
    {
        var rejected = (await Get("/api/sketches?status=Rejected&limit=1")).EnumerateArray().First();
        var id = rejected.GetProperty("id").GetString()!;
        var historyBefore = (await Get($"/api/sketches/{id}")).GetProperty("history").GetArrayLength();

        var response = await Post(Body(id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var detail = await Json(response);
        Assert.Equal("Published", detail.GetProperty("summary").GetProperty("status").GetString());
        Assert.Equal(24, detail.GetProperty("summary").GetProperty("lengthFeet").GetDouble());

        var history = detail.GetProperty("history").EnumerateArray().ToList();
        Assert.Equal(historyBefore + 1, history.Count);
        Assert.Equal("Rejected", history[^1].GetProperty("from").GetString());
        Assert.Equal("Published", history[^1].GetProperty("to").GetString());
    }

    [Theory]
    [InlineData("nodes")]
    [InlineData("discipline")]
    [InlineData("id")]
    public async Task Publish_InvalidSketch_Returns400NamingTheField(string field)
    {
        var id = NewId();
        var json = field switch
        {
            "nodes" => Body(id, nodes: "[{\"x\":0.0,\"y\":10.0,\"z\":0.0}]"),
            "discipline" => Body(id, discipline: "Plumbing"),
            _ => Body("  ")
        };

        var response = await Post(json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await Json(response);
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("error").GetString()));
        Assert.Contains(body.GetProperty("failures").EnumerateArray(), f => f.GetProperty("field").GetString() == field);

        if (field != "id")
        {
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/sketches/{id}")).StatusCode);
        }
    }

    [Fact]
    public async Task Publish_ClientCannotSetServerOwnedFields()
    {
        var id = NewId();

        var response = await Post(Body(id, extra: ",\"status\":\"Approved\",\"reviewNotes\":\"looks great\""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var detail = await Json(response);
        Assert.Equal("Published", detail.GetProperty("summary").GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("reviewNotes").ValueKind);
    }
}
