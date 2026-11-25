using Microsoft.EntityFrameworkCore;
using MiPos.Application.Common.Interfaces;
using MiPos.Domain.Entities.Typologies;

namespace MiPos.Infrastructure.Persistence;

public sealed class MiPosDbContext(DbContextOptions<MiPosDbContext> options)
    : DbContext(options), IMiPosDbContext
{
    public DbSet<Typology> Typologies => Set<Typology>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MiPosDbContext).Assembly);
    }
}
