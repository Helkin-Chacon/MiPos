namespace MiPos.Application.Typologies.Responses;

public sealed record TypologyResponse(
    int Id,
    int GroupId,
    string Code,
    string Name,
    string? Description,
    int SortOrder,
    bool IsActive);
