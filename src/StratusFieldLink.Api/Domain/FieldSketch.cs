namespace StratusFieldLink.Api.Domain;

/// <summary>
/// A run of pipe or duct sketched in the field and published to the shop.
/// </summary>
public sealed class FieldSketch
{
    /// <summary>Client-generated id. The Unity app owns this so publishes are idempotent.</summary>
    public string Id { get; set; } = "";

    public string PackageId { get; set; } = "";
    public string CrewId { get; set; } = "";

    /// <summary>Piping | SheetMetal | Electrical</summary>
    public string Discipline { get; set; } = "Piping";

    public double DiameterInches { get; set; }
    public string Material { get; set; } = "";

    public List<SketchNode> Nodes { get; set; } = new();

    public SketchStatus Status { get; set; } = SketchStatus.Published;

    public DateTime CapturedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }

    /// <summary>
    /// Run length as computed by the Unity client at capture time and sent up with
    /// the publish. Kept alongside the server-side number so the shop can see when
    /// the two disagree.
    /// </summary>
    public double ClientLengthFeet { get; set; }

    /// <summary>Version string reported by the publishing client, e.g. "field-1.4.2".</summary>
    public string ClientVersion { get; set; } = "";

    public string? ReviewNotes { get; set; }

    public List<StatusChange> History { get; set; } = new();

    /// <summary>Authoritative run length, computed from <see cref="Nodes"/>.</summary>
    public double LengthFeet => RunGeometry.TotalLengthFeet(Nodes);
}
