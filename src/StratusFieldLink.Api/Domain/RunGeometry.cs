namespace StratusFieldLink.Api.Domain;

/// <summary>
/// Server-side authority for run measurements. Anything that quotes footage to the
/// shop should come through here.
/// </summary>
public static class RunGeometry
{
    /// <summary>A run shorter than this is treated as a mis-tap, not a real sketch.</summary>
    public const double MinimumRunLengthFeet = 0.5;

    public static double TotalLengthFeet(IReadOnlyList<SketchNode>? nodes)
    {
        if (nodes is null || nodes.Count < 2)
        {
            return 0d;
        }

        var total = 0d;
        for (var i = 1; i < nodes.Count; i++)
        {
            var a = nodes[i - 1];
            var b = nodes[i];
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var dz = b.Z - a.Z;
            total += Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        }

        return total;
    }

    public static int SegmentCount(IReadOnlyList<SketchNode>? nodes)
        => nodes is null || nodes.Count < 2 ? 0 : nodes.Count - 1;

    /// <summary>Highest point minus lowest point, in feet. Zero for a flat run.</summary>
    public static double ElevationChangeFeet(IReadOnlyList<SketchNode>? nodes)
    {
        if (nodes is null || nodes.Count == 0)
        {
            return 0d;
        }

        var min = nodes.Min(n => n.Y);
        var max = nodes.Max(n => n.Y);
        return max - min;
    }
}
