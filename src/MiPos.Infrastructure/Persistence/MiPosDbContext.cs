using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using MiPos.Application.Common.Exceptions;
using MiPos.Application.Common.Interfaces;
using MiPos.Domain.Entities.Organization;
using MiPos.Domain.Entities.Typologies;
using MiPos.Infrastructure.Persistence.Configurations;
using Npgsql;

namespace MiPos.Infrastructure.Persistence;

public class MiPosDbContext(DbContextOptions<MiPosDbContext> options)
    : DbContext(options), IMiPosDbContext
{
    public DbSet<Typology> Typologies => Set<Typology>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Branch> Branches => Set<Branch>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // The DBML defines indexes explicitly.
        configurationBuilder.Conventions.Remove(typeof(ForeignKeyIndexConvention));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MiPosDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } error)
        {
            switch (error.ConstraintName)
            {
                case BusinessConfiguration.CodeIndexName:
                    throw new ConflictException("Código de negocio duplicado", "Ya existe un negocio con ese código.");
                case BranchConfiguration.CodeIndexName:
                    throw new ConflictException("Código de establecimiento duplicado",
                        "Ya existe un establecimiento con ese código en el negocio.");
                default:
                    throw;
            }
        }
    }
}
