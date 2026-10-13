namespace StratusFieldLink.Api.Domain;

public sealed record ValidationFailure(string Field, string Message);

public sealed class ValidationResult
{
    public List<ValidationFailure> Failures { get; } = new();
    public bool IsValid => Failures.Count == 0;

    public void Add(string field, string message) => Failures.Add(new ValidationFailure(field, message));
}

/// <summary>
/// Server-side gate on anything a field client tries to publish.
/// The Unity client runs an equivalent check locally so the foreman gets
/// feedback before they lose signal; this one is the authority.
/// </summary>
public static class SketchValidator
{
    public static readonly string[] KnownDisciplines = { "Piping", "SheetMetal", "Electrical" };

    public static ValidationResult Validate(FieldSketch sketch)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(sketch.Id))
        {
            result.Add("id", "A client-generated id is required so publishes are idempotent.");
        }

        if (string.IsNullOrWhiteSpace(sketch.PackageId))
        {
            result.Add("packageId", "Every sketch belongs to a package.");
        }

        if (string.IsNullOrWhiteSpace(sketch.CrewId))
        {
            result.Add("crewId", "Every sketch is attributed to a crew.");
        }

        if (!KnownDisciplines.Contains(sketch.Discipline))
        {
            result.Add("discipline", $"Unknown discipline '{sketch.Discipline}'.");
        }

        if (sketch.DiameterInches <= 0)
        {
            result.Add("diameterInches", "Diameter must be greater than zero.");
        }

        if (sketch.Nodes is null || sketch.Nodes.Count < 2)
        {
            result.Add("nodes", "A run needs at least two nodes.");
            return result;
        }

        var length = RunGeometry.TotalLengthFeet(sketch.Nodes);
        if (length < RunGeometry.MinimumRunLengthFeet)
        {
            result.Add("nodes", $"Run length {length:0.##} ft is below the {RunGeometry.MinimumRunLengthFeet} ft minimum.");
        }

        return result;
    }
}
