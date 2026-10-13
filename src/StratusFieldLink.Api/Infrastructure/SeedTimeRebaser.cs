using StratusFieldLink.Api.Domain;

namespace StratusFieldLink.Api.Infrastructure;

/// <summary>
/// The seed file is checked in with fixed timestamps so the dataset is reproducible.
/// That would make every sketch look weeks stale by the time anyone runs it, which
/// makes the review-SLA numbers on the dashboard meaningless. So on load we slide
/// the whole dataset forward as a block: the newest sketch becomes "an hour ago",
/// and every other timestamp keeps its relative position.
///
/// The shape of the data does not change. Only the offset does.
/// </summary>
public static class SeedTimeRebaser
{
    public static readonly TimeSpan NewestSketchAge = TimeSpan.FromHours(1);

    public static IReadOnlyList<FieldSketch> RebaseToNow(IReadOnlyList<FieldSketch> sketches, DateTime utcNow)
    {
        if (sketches.Count == 0)
        {
            return sketches;
        }

        var newest = sketches.Max(s => s.PublishedAtUtc ?? s.CapturedAtUtc);
        var offset = (utcNow - NewestSketchAge) - newest;

        foreach (var sketch in sketches)
        {
            sketch.CapturedAtUtc = sketch.CapturedAtUtc + offset;
            if (sketch.PublishedAtUtc.HasValue)
            {
                sketch.PublishedAtUtc = sketch.PublishedAtUtc.Value + offset;
            }

            foreach (var change in sketch.History)
            {
                change.AtUtc = change.AtUtc + offset;
            }
        }

        return sketches;
    }
}
