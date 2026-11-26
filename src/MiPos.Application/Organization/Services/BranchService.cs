using Microsoft.EntityFrameworkCore;
using MiPos.Application.Common.Exceptions;
using MiPos.Application.Common.Interfaces;
using MiPos.Application.Organization.Interfaces;
using MiPos.Application.Organization.Requests;
using MiPos.Application.Organization.Responses;
using MiPos.Domain.Entities.Organization;
using MiPos.Domain.Enums.Typologies;

namespace MiPos.Application.Organization.Services;

public class BranchService(IMiPosDbContext dbContext) : IBranchService
{
    public async Task<List<BranchResponse>> GetBranchesByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken)
    {
        await GetBusinessAsync(businessId, cancellationToken);

        var branches = await dbContext.Branches.AsNoTracking().Include(branch => branch.Business)
            .Where(branch => branch.BusinessId == businessId && branch.DeletedAt == null && branch.Business.DeletedAt == null)
            .OrderBy(branch => branch.Code)
            .ToListAsync(cancellationToken);

        return branches.ConvertAll(ToResponse);
    }

    public async Task<BranchResponse> GetBranchByIdAsync(Guid branchId, CancellationToken cancellationToken)
    {
        var branch = await dbContext.Branches.AsNoTracking().Include(branch => branch.Business)
            .SingleOrDefaultAsync(branch => branch.Id == branchId && branch.DeletedAt == null && branch.Business.DeletedAt == null,
                cancellationToken)
            ?? throw new NotFoundException("Establecimiento no encontrado", "No existe un establecimiento con ese identificador.");

        return ToResponse(branch);
    }

    public async Task<BranchResponse> CreateBranchAsync(
        Guid businessId, CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var business = await GetBusinessAsync(businessId, cancellationToken);
        if (!business.IsActive)
        {
            throw new ConflictException("Negocio inactivo", "Activa el negocio antes de crear un establecimiento.");
        }

        var operationModeId = await GetOperationModeIdAsync(request.OperationModeId, cancellationToken);
        var timeZone = GetTimeZone(request.TimeZone);
        var now = DateTime.UtcNow;
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Code = request.Code,
            Name = request.Name,
            Address = request.Address,
            Phone = request.Phone,
            OperationModeId = operationModeId,
            UsesTables = request.UsesTables,
            InventoryEnabled = request.InventoryEnabled,
            TimeZone = timeZone,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(branch, business.TimeZone);
    }

    private async Task<Business> GetBusinessAsync(Guid businessId, CancellationToken cancellationToken)
    {
        return await dbContext.Businesses.AsNoTracking()
            .SingleOrDefaultAsync(business => business.Id == businessId && business.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("Negocio no encontrado", "No existe un negocio con ese identificador.");
    }

    private async Task<int> GetOperationModeIdAsync(int? requestedId, CancellationToken cancellationToken)
    {
        var id = requestedId ?? (int)OperationMode.CashRegisterOnly;
        var isValid = await dbContext.Typologies.AsNoTracking().AnyAsync(
            typology => typology.Id == id && typology.GroupId == (int)TypologyGroup.OperationMode && typology.IsActive,
            cancellationToken);

        if (!isValid)
        {
            throw new BusinessValidationException("Modalidad de operación inválida",
                "Selecciona una modalidad de operación activa.");
        }

        return id;
    }

    private static string? GetTimeZone(string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            return null;
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _))
        {
            throw new BusinessValidationException("Zona horaria inválida",
                "Indica una zona horaria válida o deja el campo vacío para usar la del negocio.");
        }

        return timeZone;
    }

    private static BranchResponse ToResponse(Branch branch) => ToResponse(branch, branch.Business.TimeZone);

    private static BranchResponse ToResponse(Branch branch, string businessTimeZone)
    {
        return new BranchResponse
        {
            Id = branch.Id,
            BusinessId = branch.BusinessId,
            Code = branch.Code,
            Name = branch.Name,
            Address = branch.Address,
            Phone = branch.Phone,
            OperationModeId = branch.OperationModeId,
            UsesTables = branch.UsesTables,
            InventoryEnabled = branch.InventoryEnabled,
            TimeZone = string.IsNullOrWhiteSpace(branch.TimeZone) ? businessTimeZone : branch.TimeZone,
            IsActive = branch.IsActive,
            CreatedAt = branch.CreatedAt,
            UpdatedAt = branch.UpdatedAt,
            Version = branch.Version
        };
    }
}
