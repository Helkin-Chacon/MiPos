using System.ComponentModel.DataAnnotations;

namespace MiPos.Application.Identity.Requests;

public class UpdateProfileRequest
{
    [Required, StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
