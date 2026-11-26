using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPos.Domain.Entities.Organization;
using MiPos.Domain.Entities.Typologies;

namespace MiPos.Infrastructure.Persistence.Configurations;

internal class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    internal const string CodeIndexName = "ux_branches_business_id_code";

    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(entity => entity.BusinessId).HasColumnName("business_id");
        builder.Property(entity => entity.Code).HasColumnName("code").HasColumnType("varchar").IsRequired();
        builder.Property(entity => entity.Name).HasColumnName("name").HasColumnType("varchar").IsRequired();
        builder.Property(entity => entity.Address).HasColumnName("address").HasColumnType("varchar");
        builder.Property(entity => entity.Phone).HasColumnName("phone").HasColumnType("varchar");
        builder.Property(entity => entity.OperationModeId).HasColumnName("operation_mode_id");
        builder.Property(entity => entity.UsesTables).HasColumnName("uses_tables").HasDefaultValue(false);
        builder.Property(entity => entity.InventoryEnabled).HasColumnName("inventory_enabled").HasDefaultValue(false);
        builder.Property(entity => entity.TimeZone).HasColumnName("time_zone").HasColumnType("varchar");
        builder.Property(entity => entity.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(entity => entity.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(entity => entity.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(entity => entity.DeletedAt).HasColumnName("deleted_at");
        builder.Property(entity => entity.Version).HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(entity => new { entity.Id, entity.BusinessId }).IsUnique();
        builder.HasIndex(entity => new { entity.BusinessId, entity.Code }).IsUnique().HasDatabaseName(CodeIndexName);
        builder.HasOne(entity => entity.Business).WithMany().HasForeignKey(entity => entity.BusinessId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("fk_branches_business_id");
        builder.HasOne<Typology>().WithMany().HasForeignKey(entity => entity.OperationModeId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("fk_branches_operation_mode_id");
    }
}
