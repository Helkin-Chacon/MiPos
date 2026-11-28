using System.ComponentModel.DataAnnotations;

namespace MiPos.Application.Identity.Requests;

public class UpdateUserStatusRequest
{
    [Required]
    public bool? IsActive { get; set; }
}
