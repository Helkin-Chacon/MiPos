using System.ComponentModel.DataAnnotations;

namespace MiPos.Application.Identity.Requests;

public class CreateUserRequest : UpdateUserRequest
{
    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;
}
