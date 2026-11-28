using System.ComponentModel.DataAnnotations;

namespace MiPos.Application.Organization.Requests;

public class CreateBusinessRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required, RegularExpression("^[A-Za-z]{2}$")]
    public string CountryCode { get; set; } = string.Empty;
    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public string CurrencyCode { get; set; } = string.Empty;
    [Required]
    public string TimeZone { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TaxIdentification { get; set; }
    public bool IsActive { get; set; } = true;

    [Required]
    public InitialAdministratorRequest Administrator { get; set; } = null!;
}
