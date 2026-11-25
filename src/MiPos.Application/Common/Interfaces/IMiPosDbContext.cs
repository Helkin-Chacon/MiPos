using Microsoft.EntityFrameworkCore;
using MiPos.Domain.Entities.Typologies;

namespace MiPos.Application.Common.Interfaces;

public interface IMiPosDbContext
{
    DbSet<Typology> Typologies { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
