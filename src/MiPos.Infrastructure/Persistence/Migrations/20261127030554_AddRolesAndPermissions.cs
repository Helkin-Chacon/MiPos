using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MiPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "varchar", nullable: false),
                    name = table.Column<string>(type: "varchar", nullable: false),
                    description = table.Column<string>(type: "varchar", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "varchar", nullable: false),
                    name = table.Column<string>(type: "varchar", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.UniqueConstraint("AK_roles_id_business_id", x => new { x.id, x.business_id });
                    table.ForeignKey(
                        name: "fk_roles_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.business_id, x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permissions_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_role_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_role_permissions_role_id",
                        columns: x => new { x.role_id, x.business_id },
                        principalTable: "roles",
                        principalColumns: new[] { "id", "business_id" });
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "id", "code", "description", "name" },
                values: new object[,]
                {
                    { new Guid("2afadaf0-33f4-4623-b734-c4e8d917dbd2"), "CASH_REGISTER_OPEN_CLOSE", "Apertura y cierre de caja.", "Abrir y cerrar caja" },
                    { new Guid("2e8e8006-ac21-400e-aabd-56121c21adda"), "SANITATION_ACCESS", "Acceso al módulo de sanidad.", "Sanidad" },
                    { new Guid("49055b32-382d-4697-b53f-aff65296184e"), "BILLING_ACCESS", "Acceso a todos los apartados de facturación electrónica.", "Facturación electrónica" },
                    { new Guid("628405a6-f077-4153-af03-893f17fc8ea4"), "REPORTS_ACCESS", "Acceso a todos los reportes.", "Reportes" },
                    { new Guid("7b94d819-6dfc-46f2-8298-9866761de324"), "CASH_REGISTER_MOVEMENTS", "Registro de ingresos y salidas de caja.", "Movimientos de caja" },
                    { new Guid("ab044f9f-e469-4b36-993d-a161577de166"), "CATALOG_ACCESS", "Gestión de categorías, productos y combos.", "Catálogo" },
                    { new Guid("ce203714-a5e9-47cb-bfb6-8b545844d874"), "SETTINGS_ACCESS", "Configuración del negocio, operación del POS, métodos de pago e impresión.", "Configuración" },
                    { new Guid("ce909594-b90c-4804-a9c7-34a32f952387"), "ADMIN_ACCESS", "Acceso al panel administrativo.", "Acceso al administrador" },
                    { new Guid("e2aa09a2-9a8d-42fd-827c-5069cb54d3d2"), "POS_SELL", "Gestión de mesas, pedidos, ventas y cobro.", "Vender en caja" },
                    { new Guid("e8e229be-d2c9-44ac-acbe-accf4e8db0a5"), "USERS_AND_ROLES_MANAGE", "Administración de usuarios, roles y permisos.", "Usuarios y permisos" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_business_id_code",
                table: "roles",
                columns: new[] { "business_id", "code" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO roles (id, business_id, code, name)
                SELECT gen_random_uuid(), businesses.id, defaults.code, defaults.name
                FROM businesses
                CROSS JOIN (VALUES ('ADMINISTRATOR', 'Administrador'), ('CASHIER', 'Cajero')) AS defaults(code, name)
                ON CONFLICT (business_id, code) DO NOTHING;

                INSERT INTO role_permissions (business_id, role_id, permission_id)
                SELECT roles.business_id, roles.id, permissions.id
                FROM roles
                CROSS JOIN permissions
                WHERE roles.code = 'ADMINISTRATOR'
                   OR (roles.code = 'CASHIER' AND permissions.code IN ('POS_SELL', 'CASH_REGISTER_OPEN_CLOSE'))
                ON CONFLICT (business_id, role_id, permission_id) DO NOTHING;
                """);

            migrationBuilder.Sql("""
                CREATE FUNCTION grant_permission_to_administrators() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    INSERT INTO role_permissions (business_id, role_id, permission_id)
                    SELECT business_id, id, NEW.id
                    FROM roles
                    WHERE code = 'ADMINISTRATOR'
                    ON CONFLICT (business_id, role_id, permission_id) DO NOTHING;
                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER permissions_grant_to_administrators
                AFTER INSERT ON permissions
                FOR EACH ROW EXECUTE FUNCTION grant_permission_to_administrators();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER permissions_grant_to_administrators ON permissions;
                DROP FUNCTION grant_permission_to_administrators();
                """);

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
