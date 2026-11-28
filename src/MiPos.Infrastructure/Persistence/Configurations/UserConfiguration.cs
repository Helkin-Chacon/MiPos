using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPos.Domain.Entities.Identity;
using MiPos.Domain.Entities.Organization;

namespace MiPos.Infrastructure.Persistence.Configurations;

internal class UserConfiguration : IEntityTypeConfiguration<User>
{
    internal const string UsernameIndexName = "ux_users_normalized_username";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.HasIndex(user => new { user.Id, user.BusinessId }).IsUnique();
        builder.HasIndex(user => user.NormalizedUsername).IsUnique().HasDatabaseName(UsernameIndexName);

        builder.Property(user => user.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(user => user.BusinessId).HasColumnName("business_id");
        builder.Property(user => user.RoleId).HasColumnName("role_id");
        builder.Property(user => user.Username).HasColumnName("username").HasColumnType("varchar");
        builder.Property(user => user.NormalizedUsername).HasColumnName("normalized_username").HasColumnType("varchar");
        builder.Property(user => user.FirstName).HasColumnName("first_name").HasColumnType("varchar");
        builder.Property(user => user.LastName).HasColumnName("last_name").HasColumnType("varchar");
        builder.Property(user => user.Email).HasColumnName("email").HasColumnType("varchar");
        builder.Property(user => user.PasswordHash).HasColumnName("password_hash").HasColumnType("varchar");
        builder.Property(user => user.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(user => user.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(user => user.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(user => user.DeletedAt).HasColumnName("deleted_at");
        builder.Property(user => user.Version).HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken();

        builder.HasOne<Business>().WithMany().HasForeignKey(user => user.BusinessId)
            .HasConstraintName("fk_users_business_id").OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Role>().WithMany().HasForeignKey(user => new { user.RoleId, user.BusinessId })
            .HasPrincipalKey(role => new { role.Id, role.BusinessId })
            .HasConstraintName("fk_users_role_id").OnDelete(DeleteBehavior.NoAction);
    }
}
