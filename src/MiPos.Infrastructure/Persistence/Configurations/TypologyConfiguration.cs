using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPos.Domain.Entities.Typologies;
using MiPos.Infrastructure.Persistence.Seed;

namespace MiPos.Infrastructure.Persistence.Configurations;

internal class TypologyConfiguration : IEntityTypeConfiguration<Typology>
{
    public void Configure(EntityTypeBuilder<Typology> builder)
    {
        builder.ToTable("typologies");
        builder.HasKey(typology => typology.Id);
        builder.Property(typology => typology.Id).HasColumnName("id").HasColumnType("integer").ValueGeneratedNever();
        builder.Property(typology => typology.GroupId).HasColumnName("group_id").HasColumnType("integer");
        builder.Property(typology => typology.Code).HasColumnName("code").HasColumnType("varchar").IsRequired();
        builder.Property(typology => typology.Name).HasColumnName("name").HasColumnType("varchar").IsRequired();
        builder.Property(typology => typology.Description).HasColumnName("description").HasColumnType("varchar");
        builder.Property(typology => typology.SortOrder).HasColumnName("sort_order").HasColumnType("integer").HasDefaultValue(0);
        builder.Property(typology => typology.IsActive).HasColumnName("is_active").HasColumnType("boolean").HasDefaultValue(true);
        builder.HasIndex(typology => new { typology.GroupId, typology.Code }).IsUnique();
        builder.HasData(TypologySeed.Values);
    }
}
