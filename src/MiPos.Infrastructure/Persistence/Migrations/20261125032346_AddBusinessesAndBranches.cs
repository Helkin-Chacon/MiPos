using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessesAndBranches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "businesses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "varchar", nullable: false),
                    name = table.Column<string>(type: "varchar", nullable: false),
                    legal_name = table.Column<string>(type: "varchar", nullable: true),
                    tax_identification = table.Column<string>(type: "varchar", nullable: true),
                    country_code = table.Column<string>(type: "char(2)", nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", nullable: false),
                    time_zone = table.Column<string>(type: "varchar", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_businesses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "varchar", nullable: false),
                    name = table.Column<string>(type: "varchar", nullable: false),
                    address = table.Column<string>(type: "varchar", nullable: true),
                    phone = table.Column<string>(type: "varchar", nullable: true),
                    operation_mode_id = table.Column<int>(type: "integer", nullable: false),
                    uses_tables = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    inventory_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    time_zone = table.Column<string>(type: "varchar", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.id);
                    table.ForeignKey(
                        name: "fk_branches_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_branches_operation_mode_id",
                        column: x => x.operation_mode_id,
                        principalTable: "typologies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_branches_id_business_id",
                table: "branches",
                columns: new[] { "id", "business_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_branches_business_id_code",
                table: "branches",
                columns: new[] { "business_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_businesses_code",
                table: "businesses",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropTable(
                name: "businesses");
        }
    }
}
