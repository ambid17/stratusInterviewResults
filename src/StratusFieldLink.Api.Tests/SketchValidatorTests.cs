using StratusFieldLink.Api.Domain;
using Xunit;

namespace StratusFieldLink.Api.Tests;

public class SketchValidatorTests
{
    private static FieldSketch Valid() => new()
    {
        Id = "SK-9001",
        PackageId = "PKG-CHW-L02",
        CrewId = "CREW-01",
        Discipline = "Piping",
        DiameterInches = 4,
        Material = "Carbon Steel",
        Nodes = new List<SketchNode>
        {
            new() { X = 0, Y = 10, Z = 0 },
            new() { X = 24, Y = 10, Z = 0 }
        }
    };

    [Fact]
    public void Validate_WellFormedSketch_Passes()
    {
        Assert.True(SketchValidator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void Validate_SingleNode_Fails()
    {
        var sketch = Valid();
        sketch.Nodes = new List<SketchNode> { new() { X = 0, Y = 10, Z = 0 } };

        var result = SketchValidator.Validate(sketch);

        Assert.False(result.IsValid);
        Assert.Contains(result.Failures, f => f.Field == "nodes");
    }

    [Fact]
    public void Validate_UnknownDiscipline_Fails()
    {
        var sketch = Valid();
        sketch.Discipline = "Plumbing";

        Assert.Contains(SketchValidator.Validate(sketch).Failures, f => f.Field == "discipline");
    }

    [Fact]
    public void Validate_MissingPackage_Fails()
    {
        var sketch = Valid();
        sketch.PackageId = "  ";

        Assert.Contains(SketchValidator.Validate(sketch).Failures, f => f.Field == "packageId");
    }

    [Fact]
    public void Validate_TwoNodesInTheSameSpot_Fails()
    {
        var sketch = Valid();
        sketch.Nodes = new List<SketchNode>
        {
            new() { X = 5, Y = 10, Z = 5 },
            new() { X = 5, Y = 10, Z = 5 }
        };

        Assert.Contains(SketchValidator.Validate(sketch).Failures, f => f.Field == "nodes");
    }
}
