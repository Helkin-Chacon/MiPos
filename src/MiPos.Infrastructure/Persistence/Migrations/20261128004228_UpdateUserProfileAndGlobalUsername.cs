using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserProfileAndGlobalUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                LOCK TABLE users IN ACCESS EXCLUSIVE MODE;

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT normalized_username
                        FROM users
                        GROUP BY normalized_username
                        HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Resolve duplicate normalized usernames before applying this migration.';
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM users
                        WHERE email IS NULL OR email !~ '^[^@[:space:]]+@[^@[:space:]]+$'
                    ) THEN
                        RAISE EXCEPTION 'Set a valid email for every existing user before applying this migration.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_users_business_id_normalized_username",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "users",
                newName: "first_name");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "varchar",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                table: "users",
                type: "varchar",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_users_normalized_username",
                table: "users",
                column: "normalized_username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE users
                SET first_name = first_name || ' ' || last_name
                WHERE last_name IS NOT NULL AND last_name <> '';
                """);

            migrationBuilder.DropIndex(
                name: "ux_users_normalized_username",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_name",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "first_name",
                table: "users",
                newName: "name");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "varchar",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar");

            migrationBuilder.CreateIndex(
                name: "IX_users_business_id_normalized_username",
                table: "users",
                columns: new[] { "business_id", "normalized_username" },
                unique: true);
        }
    }
}
