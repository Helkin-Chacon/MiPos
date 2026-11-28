namespace MiPos.Application.Identity.Responses;

public class RoleResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<PermissionResponse> Permissions { get; set; } = [];
}
