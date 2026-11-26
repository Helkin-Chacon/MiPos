using System.ComponentModel.DataAnnotations;

namespace MiPos.Application.Organization.Requests;

public class CreateBranchRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public int? OperationModeId { get; set; }
    public bool UsesTables { get; set; }
    public bool InventoryEnabled { get; set; }
    public string? TimeZone { get; set; }
    public bool IsActive { get; set; } = true;
}
