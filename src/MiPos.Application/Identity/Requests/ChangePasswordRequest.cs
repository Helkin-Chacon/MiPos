using System.ComponentModel.DataAnnotations;

namespace MiPos.Application.Identity.Requests;

public class ChangePasswordRequest
{
    [Required, StringLength(128)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}
