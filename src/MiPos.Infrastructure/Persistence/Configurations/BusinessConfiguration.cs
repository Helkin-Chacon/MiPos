using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPos.Domain.Entities.Organization;

namespace MiPos.Infrastructure.Persistence.Configurations;

internal class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    internal const string CodeIndexName = "ux_businesses_code";

    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("businesses");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(entity => entity.Code).HasColumnName("code").HasColumnType("varchar").IsRequired();
        builder.Property(entity => entity.Name).HasColumnName("name").HasColumnType("varchar").IsRequired();
        builder.Property(entity => entity.LegalName).HasColumnName("legal_name").HasColumnType("varchar");
        builder.Property(entity => entity.TaxIdentification).HasColumnName("tax_identification").HasColumnType("varchar");
        builder.Property(entity => entity.CountryCode).HasColumnName("country_code").HasColumnType("char(2)").IsRequired();
        builder.Property(entity => entity.CurrencyCode).HasColumnName("currency_code").HasColumnType("char(3)").IsRequired();
        builder.Property(entity => entity.TimeZone).HasColumnName("time_zone").HasColumnType("varchar").IsRequired();
        builder.Property(entity => entity.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(entity => entity.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(entity => entity.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(entity => entity.DeletedAt).HasColumnName("deleted_at");
        builder.Property(entity => entity.Version).HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasIndex(entity => entity.Code).IsUnique().HasDatabaseName(CodeIndexName);
    }
}
