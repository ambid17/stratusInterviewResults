using StratusFieldLink.Api.Domain;

namespace StratusFieldLink.Api.Infrastructure;

public interface ISketchRepository
{
    IReadOnlyList<FieldSketch> All();
    FieldSketch? Find(string id);

    /// <summary>Insert or replace. Returns true when the sketch was new.</summary>
    bool Upsert(FieldSketch sketch);
}
