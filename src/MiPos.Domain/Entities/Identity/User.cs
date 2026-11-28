namespace MiPos.Domain.Entities.Identity;

public class User
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid RoleId { get; set; }
    public required string Username { get; set; }
    public required string NormalizedUsername { get; set; }
    public required string FirstName { get; set; }
    public string? LastName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public long Version { get; set; } = 1;

    public static string NormalizeUsername(string username) =>
        username.Trim().Normalize(System.Text.NormalizationForm.FormKC).ToUpperInvariant();
}
