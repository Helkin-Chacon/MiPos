namespace MiPos.Domain.Entities.Organization;

public class Business
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? LegalName { get; set; }
    public string? TaxIdentification { get; set; }
    public required string CountryCode { get; set; }
    public required string CurrencyCode { get; set; }
    public required string TimeZone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public long Version { get; set; } = 1;
}
