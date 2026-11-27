using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPos.Domain.Entities.Identity;
using MiPos.Domain.Entities.Organization;

namespace MiPos.Infrastructure.Persistence.Configurations;

internal class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.Id, entity.BusinessId });
        builder.HasIndex(entity => new { entity.BusinessId, entity.Code }).IsUnique();

        builder.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(entity => entity.BusinessId).HasColumnName("business_id");
        builder.Property(entity => entity.Code).HasColumnName("code").HasColumnType("varchar").IsRequired();
        builder.Property(entity => entity.Name).HasColumnName("name").HasColumnType("varchar").IsRequired();
        builder.Property(entity => entity.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(entity => entity.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(entity => entity.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(entity => entity.DeletedAt).HasColumnName("deleted_at");
        builder.Property(entity => entity.Version).HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasOne<Business>().WithMany().HasForeignKey(entity => entity.BusinessId)
            .HasConstraintName("fk_roles_business_id").OnDelete(DeleteBehavior.NoAction);
    }
}
