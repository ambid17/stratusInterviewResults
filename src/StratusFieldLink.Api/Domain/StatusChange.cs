namespace StratusFieldLink.Api.Domain;

public sealed class StatusChange
{
    public SketchStatus From { get; set; }
    public SketchStatus To { get; set; }
    public DateTime AtUtc { get; set; }
    public string Actor { get; set; } = "";
}
