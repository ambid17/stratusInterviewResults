using StratusFieldLink.Api.Domain;
using StratusFieldLink.Api.Infrastructure;

namespace StratusFieldLink.Api.Api;

public static class SketchEndpoints
{
    public static IEndpointRouteBuilder MapSketchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }));

        app.MapGet("/api/sketches", (
            ISketchRepository repo,
            string? status,
            string? packageId,
            string? crewId,
            int? limit) =>
        {
            IEnumerable<FieldSketch> query = repo.All();

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<SketchStatus>(status, ignoreCase: true, out var parsed))
                {
                    return Results.BadRequest(new { error = $"Unknown status '{status}'." });
                }

                query = query.Where(s => s.Status == parsed);
            }

            if (!string.IsNullOrWhiteSpace(packageId))
            {
                query = query.Where(s => string.Equals(s.PackageId, packageId, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(crewId))
            {
                query = query.Where(s => string.Equals(s.CrewId, crewId, StringComparison.OrdinalIgnoreCase));
            }

            var results = query
                .OrderByDescending(s => s.PublishedAtUtc ?? s.CapturedAtUtc)
                .Take(limit is > 0 and <= 500 ? limit.Value : 100)
                .Select(s => s.ToSummary())
                .ToList();

            return Results.Ok(results);
        });

        app.MapGet("/api/sketches/{id}", (ISketchRepository repo, string id) =>
        {
            var sketch = repo.Find(id);
            return sketch is null
                ? Results.NotFound(new { error = $"No sketch '{id}'." })
                : Results.Ok(sketch.ToDetail());
        });

        // Publish from the Unity field client. The id is client-generated, so the
        // tablet can retry safely when it comes back into signal:
        //   new id                          -> 201
        //   same id, same run (a retry)     -> 200, nothing rewritten
        //   same id, currently Rejected     -> 200, republished after rework
        //   same id, different run          -> 409
        //   fails SketchValidator           -> 400
        app.MapPost("/api/sketches", (PublishSketchRequest request, ISketchRepository repo) =>
        {
            var sketch = request.ToFieldSketch();

            var validation = SketchValidator.Validate(sketch);
            if (!validation.IsValid)
            {
                return Results.BadRequest(new
                {
                    error = string.Join(" ", validation.Failures.Select(f => f.Message)),
                    failures = validation.Failures
                });
            }

            var now = DateTime.UtcNow;
            sketch.Status = SketchStatus.Published;
            sketch.PublishedAtUtc = now;

            var existing = repo.Find(sketch.Id);

            if (existing is null)
            {
                sketch.History.Add(new StatusChange
                {
                    From = SketchStatus.Draft,
                    To = SketchStatus.Published,
                    AtUtc = now,
                    Actor = sketch.CrewId
                });

                // Upsert returns false if a concurrent retry of the same publish won the race.
                return repo.Upsert(sketch)
                    ? Results.Created($"/api/sketches/{Uri.EscapeDataString(sketch.Id)}", sketch.ToDetail())
                    : Results.Ok(sketch.ToDetail());
            }

            if (existing.SameContentAs(sketch))
            {
                return Results.Ok(existing.ToDetail());
            }

            return Results.Conflict(new
            {
                error = $"Sketch '{existing.Id}' was already published with different content and is {existing.Status}."
            });
        });

        return app;
    }
}
