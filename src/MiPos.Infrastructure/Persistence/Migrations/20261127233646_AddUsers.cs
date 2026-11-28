using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "varchar", nullable: false),
                    normalized_username = table.Column<string>(type: "varchar", nullable: false),
                    name = table.Column<string>(type: "varchar", nullable: false),
                    email = table.Column<string>(type: "varchar", nullable: true),
                    password_hash = table.Column<string>(type: "varchar", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_users_role_id",
                        columns: x => new { x.role_id, x.business_id },
                        principalTable: "roles",
                        principalColumns: new[] { "id", "business_id" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_business_id_normalized_username",
                table: "users",
                columns: new[] { "business_id", "normalized_username" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_id_business_id",
                table: "users",
                columns: new[] { "id", "business_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
