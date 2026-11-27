namespace MiPos.Domain.Entities.Identity;

public class RolePermission
{
    public Guid BusinessId { get; set; }
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
