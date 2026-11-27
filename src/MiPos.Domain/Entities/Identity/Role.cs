namespace MiPos.Domain.Entities.Identity;

public class Role
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public long Version { get; set; } = 1;
}
