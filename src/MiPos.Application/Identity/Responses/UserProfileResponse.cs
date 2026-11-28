namespace MiPos.Application.Identity.Responses;

public class UserProfileResponse
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string FirstName { get; set; }
    public string? LastName { get; set; }
    public required string Email { get; set; }
    public Guid BusinessId { get; set; }
    public required string RoleCode { get; set; }
}
