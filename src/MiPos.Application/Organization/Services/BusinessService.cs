using Microsoft.EntityFrameworkCore;
using MiPos.Application.Common.Exceptions;
using MiPos.Application.Common.Interfaces;
using MiPos.Application.Organization.Interfaces;
using MiPos.Application.Organization.Requests;
using MiPos.Application.Organization.Responses;
using MiPos.Domain.Entities.Organization;

namespace MiPos.Application.Organization.Services;

public class BusinessService(IMiPosDbContext dbContext) : IBusinessService
{
    public async Task<BusinessResponse> GetBusinessByIdAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var business = await dbContext.Businesses.AsNoTracking()
            .SingleOrDefaultAsync(business => business.Id == businessId && business.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("Negocio no encontrado", "No existe un negocio con ese identificador.");

        return ToResponse(business);
    }

    public async Task<BusinessResponse> CreateBusinessAsync(CreateBusinessRequest request, CancellationToken cancellationToken)
    {
        ValidateTimeZone(request.TimeZone);

        var now = DateTime.UtcNow;
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            LegalName = request.LegalName,
            TaxIdentification = request.TaxIdentification,
            CountryCode = request.CountryCode,
            CurrencyCode = request.CurrencyCode,
            TimeZone = request.TimeZone,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Businesses.Add(business);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(business);
    }

    private static void ValidateTimeZone(string timeZone)
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _))
        {
            throw new BusinessValidationException("Zona horaria inválida", "Indica una zona horaria válida para el negocio.");
        }
    }

    private static BusinessResponse ToResponse(Business business)
    {
        return new BusinessResponse
        {
            Id = business.Id,
            Code = business.Code,
            Name = business.Name,
            LegalName = business.LegalName,
            TaxIdentification = business.TaxIdentification,
            CountryCode = business.CountryCode,
            CurrencyCode = business.CurrencyCode,
            TimeZone = business.TimeZone,
            IsActive = business.IsActive,
            CreatedAt = business.CreatedAt,
            UpdatedAt = business.UpdatedAt,
            Version = business.Version
        };
    }
}
