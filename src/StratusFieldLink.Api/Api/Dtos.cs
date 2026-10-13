using StratusFieldLink.Api.Domain;

namespace StratusFieldLink.Api.Api;

public sealed record SketchSummaryDto(
    string Id,
    string PackageId,
    string CrewId,
    string Discipline,
    double DiameterInches,
    string Material,
    int NodeCount,
    double LengthFeet,
    double ClientLengthFeet,
    double ElevationChangeFeet,
    bool LengthMismatch,
    string Status,
    DateTime CapturedAtUtc,
    DateTime? PublishedAtUtc,
    string ClientVersion);

public sealed record SketchDetailDto(
    SketchSummaryDto Summary,
    IReadOnlyList<SketchNode> Nodes,
    IReadOnlyList<StatusChange> History,
    string? ReviewNotes);

/// <summary>
/// What the Unity field client sends on publish; mirrors its FieldSketchDto.
/// Status, history and review notes are deliberately absent: the server owns those.
/// </summary>
public sealed record PublishSketchRequest(
    string? Id,
    string? PackageId,
    string? CrewId,
    string? Discipline,
    double DiameterInches,
    string? Material,
    List<SketchNode>? Nodes,
    DateTime? CapturedAtUtc,
    double ClientLengthFeet,
    string? ClientVersion);

public sealed record StationCountDto(string Key, int Count);

public sealed record DashboardDto(
    int TotalSketches,
    IReadOnlyList<StationCountDto> CountsByStatus,
    IReadOnlyList<StationCountDto> CountsByCrew,
    int NeedsReviewCount,
    int StaleReviewCount,
    int LengthMismatchCount,
    double TotalPublishedFeet,
    IReadOnlyList<SketchSummaryDto> OldestAwaitingReview);

public sealed record TokenRequestDto(string DeviceCode, string CrewId);

public sealed record TokenResponseDto(string AccessToken, string CrewId, DateTime ExpiresAtUtc);

public static class DtoMapper
{
    public const double LengthMismatchToleranceFeet = 0.5;

    public static SketchSummaryDto ToSummary(this FieldSketch s)
    {
        var serverLength = s.LengthFeet;
        return new SketchSummaryDto(
            s.Id,
            s.PackageId,
            s.CrewId,
            s.Discipline,
            s.DiameterInches,
            s.Material,
            s.Nodes.Count,
            Math.Round(serverLength, 2),
            Math.Round(s.ClientLengthFeet, 2),
            Math.Round(RunGeometry.ElevationChangeFeet(s.Nodes), 2),
            Math.Abs(serverLength - s.ClientLengthFeet) > LengthMismatchToleranceFeet,
            s.Status.ToString(),
            s.CapturedAtUtc,
            s.PublishedAtUtc,
            s.ClientVersion);
    }

    public static SketchDetailDto ToDetail(this FieldSketch s)
        => new(s.ToSummary(), s.Nodes, s.History, s.ReviewNotes);

    public static FieldSketch ToFieldSketch(this PublishSketchRequest r)
        => new()
        {
            Id = (r.Id ?? "").Trim(),
            PackageId = (r.PackageId ?? "").Trim(),
            CrewId = (r.CrewId ?? "").Trim(),
            Discipline = (r.Discipline ?? "").Trim(),
            DiameterInches = r.DiameterInches,
            Material = (r.Material ?? "").Trim(),
            Nodes = r.Nodes?.Where(n => n is not null).ToList() ?? new List<SketchNode>(),
            CapturedAtUtc = r.CapturedAtUtc ?? DateTime.UtcNow,
            ClientLengthFeet = r.ClientLengthFeet,
            ClientVersion = r.ClientVersion ?? ""
        };

    /// <summary>
    /// True when two publishes describe the same run. Compares what the foreman drew,
    /// not the stamps the client re-applies on every attempt (clientVersion,
    /// clientLengthFeet), so a retry after an app update still counts as a retry.
    /// </summary>
    public static bool SameContentAs(this FieldSketch a, FieldSketch b)
        => string.Equals(a.PackageId, b.PackageId, StringComparison.OrdinalIgnoreCase)
           && string.Equals(a.CrewId, b.CrewId, StringComparison.OrdinalIgnoreCase)
           && a.Discipline == b.Discipline
           && a.DiameterInches.Equals(b.DiameterInches)
           && a.Material == b.Material
           && a.Nodes.Count == b.Nodes.Count
           && a.Nodes.Zip(b.Nodes).All(p =>
               p.First.X.Equals(p.Second.X) && p.First.Y.Equals(p.Second.Y) && p.First.Z.Equals(p.Second.Z));
}
