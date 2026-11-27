using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPos.Domain.Entities.Identity;
using MiPos.Infrastructure.Persistence.Seed;

namespace MiPos.Infrastructure.Persistence.Configurations;

internal class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(entity => entity.Id);
        builder.HasIndex(entity => entity.Code).IsUnique();

        builder.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(entity => entity.Code).HasColumnName("code").HasColumnType("varchar").IsRequired();
        builder.Property(entity => entity.Name).HasColumnName("name").HasColumnType("varchar").IsRequired();
        builder.Property(entity => entity.Description).HasColumnName("description").HasColumnType("varchar");

        builder.HasData(PermissionSeed.Values);
    }
}
