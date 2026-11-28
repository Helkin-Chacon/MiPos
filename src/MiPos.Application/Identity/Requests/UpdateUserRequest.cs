using System.ComponentModel.DataAnnotations;

namespace MiPos.Application.Identity.Requests;

public class UpdateUserRequest : UpdateProfileRequest
{
    [Required]
    public Guid? RoleId { get; set; }
}
