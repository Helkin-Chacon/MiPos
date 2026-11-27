using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPos.Domain.Entities.Identity;
using MiPos.Domain.Entities.Organization;

namespace MiPos.Infrastructure.Persistence.Configurations;

internal class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(entity => new { entity.BusinessId, entity.RoleId, entity.PermissionId });

        builder.Property(entity => entity.BusinessId).HasColumnName("business_id");
        builder.Property(entity => entity.RoleId).HasColumnName("role_id");
        builder.Property(entity => entity.PermissionId).HasColumnName("permission_id");

        builder.HasOne<Business>().WithMany().HasForeignKey(entity => entity.BusinessId)
            .HasConstraintName("fk_role_permissions_business_id").OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Role>().WithMany().HasForeignKey(entity => new { entity.RoleId, entity.BusinessId })
            .HasPrincipalKey(entity => new { entity.Id, entity.BusinessId })
            .HasConstraintName("fk_role_permissions_role_id").OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Permission>().WithMany().HasForeignKey(entity => entity.PermissionId)
            .HasConstraintName("fk_role_permissions_permission_id").OnDelete(DeleteBehavior.NoAction);
    }
}
