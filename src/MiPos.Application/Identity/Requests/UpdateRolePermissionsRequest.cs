using System.ComponentModel.DataAnnotations;

namespace MiPos.Application.Identity.Requests;

public class UpdateRolePermissionsRequest
{
    [Required]
    public List<Guid> PermissionIds { get; set; } = null!;
}
