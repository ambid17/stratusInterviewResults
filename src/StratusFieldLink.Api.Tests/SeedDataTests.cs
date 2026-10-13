using StratusFieldLink.Api.Domain;
using StratusFieldLink.Api.Infrastructure;
using Xunit;

namespace StratusFieldLink.Api.Tests;

/// <summary>
/// Guards the checked-in seed file. If these fail, the starter dataset is broken
/// and nothing downstream will behave.
/// </summary>
public class SeedDataTests
{
    private static InMemorySketchRepository Repo() => InMemorySketchRepository.FromSeedFile(SeedLocator.Locate());

    [Fact]
    public void Seed_LoadsAndIsNotTrivial()
    {
        Assert.True(Repo().All().Count >= 100);
    }

    [Fact]
    public void Seed_EverySketchHasAtLeastTwoNodes()
    {
        Assert.All(Repo().All(), s => Assert.True(s.Nodes.Count >= 2, $"{s.Id} has {s.Nodes.Count} node(s)"));
    }

    [Fact]
    public void Seed_EverySketchIsAddressableById()
    {
        var repo = Repo();
        Assert.All(repo.All(), s => Assert.NotNull(repo.Find(s.Id)));
    }

    [Fact]
    public void Seed_RebasedSoNothingIsInTheFuture()
    {
        var now = DateTime.UtcNow;
        Assert.All(Repo().All(), s => Assert.True(s.CapturedAtUtc <= now, $"{s.Id} was captured in the future"));
    }
}
