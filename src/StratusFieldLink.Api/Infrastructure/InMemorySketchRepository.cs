using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using StratusFieldLink.Api.Domain;

namespace StratusFieldLink.Api.Infrastructure;

/// <summary>
/// Stand-in for the MongoDB collection the real service uses. Same shape, no install.
/// </summary>
public sealed class InMemorySketchRepository : ISketchRepository
{
    private readonly ConcurrentDictionary<string, FieldSketch> _sketches = new(StringComparer.OrdinalIgnoreCase);

    public static readonly JsonSerializerOptions SeedJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    public InMemorySketchRepository()
    {
    }

    public InMemorySketchRepository(IEnumerable<FieldSketch> seed)
    {
        foreach (var sketch in seed)
        {
            _sketches[sketch.Id] = sketch;
        }
    }

    public static InMemorySketchRepository FromSeedFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Seed file not found at '{path}'. Run `node scripts/generate-seed.mjs` from the repo root.", path);
        }

        using var stream = File.OpenRead(path);
        var seed = JsonSerializer.Deserialize<List<FieldSketch>>(stream, SeedJsonOptions) ?? new List<FieldSketch>();
        SeedTimeRebaser.RebaseToNow(seed, DateTime.UtcNow);
        return new InMemorySketchRepository(seed);
    }

    public IReadOnlyList<FieldSketch> All() => _sketches.Values.ToList();

    public FieldSketch? Find(string id)
        => string.IsNullOrWhiteSpace(id) ? null : _sketches.TryGetValue(id, out var s) ? s : null;

    public bool Upsert(FieldSketch sketch)
    {
        ArgumentNullException.ThrowIfNull(sketch);

        if (string.IsNullOrWhiteSpace(sketch.Id))
        {
            throw new ArgumentException("A sketch needs a client-generated id.", nameof(sketch));
        }

        if (_sketches.TryAdd(sketch.Id, sketch))
        {
            return true;
        }

        _sketches[sketch.Id] = sketch;
        return false;
    }
}
