namespace StratusFieldLink.Api.Domain;

/// <summary>
/// Lifecycle of a field sketch, from the foreman's tablet to the shop.
///
///   Draft -> Published -> UnderReview -> Approved
///                                     -> Rejected -> Published (after rework)
///
/// Draft only ever exists on the Unity client. The server never sees it.
/// </summary>
public enum SketchStatus
{
    Draft = 0,
    Published = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4
}
