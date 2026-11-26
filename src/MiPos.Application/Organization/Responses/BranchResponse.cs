namespace MiPos.Application.Organization.Responses;

public class BranchResponse
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public int OperationModeId { get; set; }
    public bool UsesTables { get; set; }
    public bool InventoryEnabled { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long Version { get; set; }
}
