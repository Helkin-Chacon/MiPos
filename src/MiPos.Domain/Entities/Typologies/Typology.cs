namespace MiPos.Domain.Entities.Typologies;

public sealed class Typology
{
    public int Id { get; init; }
    public int GroupId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;
}
