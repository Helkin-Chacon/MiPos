using System.ComponentModel.DataAnnotations;

namespace MiPos.Application.Identity.Requests;

public class LoginRequest
{
    [Required, StringLength(100)]
    public string Username { get; set; } = "";

    [Required, StringLength(128)]
    public string Password { get; set; } = "";
}
