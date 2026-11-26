using Microsoft.EntityFrameworkCore;
using MiPos.Domain.Entities.Organization;
using MiPos.Domain.Entities.Typologies;

namespace MiPos.Application.Common.Interfaces;

public interface IMiPosDbContext
{
    DbSet<Typology> Typologies { get; }
    DbSet<Business> Businesses { get; }
    DbSet<Branch> Branches { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
