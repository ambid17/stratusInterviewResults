namespace StratusFieldLink.Api.Domain;

/// <summary>
/// A single point in a sketched run, in feet, in job-site coordinates.
/// X/Z are the floor plane. Y is elevation (up).
/// </summary>
public sealed class SketchNode
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}
