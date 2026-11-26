namespace MiPos.Domain.Entities.Organization;

public class Branch
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public int OperationModeId { get; set; }
    public bool UsesTables { get; set; }
    public bool InventoryEnabled { get; set; }
    public string? TimeZone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public long Version { get; set; } = 1;
}
