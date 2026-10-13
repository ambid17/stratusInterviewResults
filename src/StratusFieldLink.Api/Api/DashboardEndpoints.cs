using StratusFieldLink.Api.Domain;
using StratusFieldLink.Api.Infrastructure;

namespace StratusFieldLink.Api.Api;

public static class DashboardEndpoints
{
    public const int ReviewSlaHours = 24;

    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", (ISketchRepository repo) =>
        {
            var all = repo.All();
            var now = DateTime.UtcNow;

            var awaitingReview = all
                .Where(s => s.Status == SketchStatus.Published)
                .OrderBy(s => s.PublishedAtUtc ?? s.CapturedAtUtc)
                .ToList();

            var dto = new DashboardDto(
                TotalSketches: all.Count,
                CountsByStatus: all
                    .GroupBy(s => s.Status.ToString())
                    .OrderBy(g => g.Key)
                    .Select(g => new StationCountDto(g.Key, g.Count()))
                    .ToList(),
                CountsByCrew: all
                    .GroupBy(s => s.CrewId)
                    .OrderBy(g => g.Key)
                    .Select(g => new StationCountDto(g.Key, g.Count()))
                    .ToList(),
                NeedsReviewCount: awaitingReview.Count,
                StaleReviewCount: awaitingReview
                    .Count(s => (now - (s.PublishedAtUtc ?? s.CapturedAtUtc)).TotalHours > ReviewSlaHours),
                LengthMismatchCount: all.Count(s =>
                    Math.Abs(s.LengthFeet - s.ClientLengthFeet) > DtoMapper.LengthMismatchToleranceFeet),
                TotalPublishedFeet: Math.Round(
                    all.Where(s => s.Status != SketchStatus.Rejected).Sum(s => s.LengthFeet), 2),
                OldestAwaitingReview: awaitingReview.Take(20).Select(s => s.ToSummary()).ToList());

            return Results.Ok(dto);
        });

        // ---------------------------------------------------------------------
        // TODO(candidate): add a throughput view.
        //
        // Roughly: how much work are crews publishing per day, and is the shop
        // keeping up with reviewing it? This is deliberately under-specified.
        // Pick a definition, write it down in PROCESS.md, and move on.
        // ---------------------------------------------------------------------

        return app;
    }
}
