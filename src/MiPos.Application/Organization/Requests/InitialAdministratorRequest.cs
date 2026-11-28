using System.ComponentModel.DataAnnotations;

namespace MiPos.Application.Organization.Requests;

public class InitialAdministratorRequest
{
    [Required, StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;
}
