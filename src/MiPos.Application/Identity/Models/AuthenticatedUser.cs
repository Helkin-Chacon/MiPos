namespace MiPos.Application.Identity.Models;

public class AuthenticatedUser
{
    public Guid UserId { get; set; }
    public Guid BusinessId { get; set; }
    public required string RoleCode { get; set; }
    public required List<string> Permissions { get; set; }
}
