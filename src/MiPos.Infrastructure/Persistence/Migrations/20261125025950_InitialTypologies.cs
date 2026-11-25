using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MiPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialTypologies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "typologies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "varchar", nullable: false),
                    name = table.Column<string>(type: "varchar", nullable: false),
                    description = table.Column<string>(type: "varchar", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_typologies", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "typologies",
                columns: new[] { "id", "code", "description", "group_id", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { 1, "CASH_REGISTER_ONLY", null, 1, true, "Solo caja", 1 },
                    { 2, "CASH_REGISTER_AND_WAITERS", null, 1, true, "Caja y meseros", 2 },
                    { 3, "CASH_REGISTER", null, 2, true, "Caja", 1 },
                    { 4, "WAITER", null, 2, true, "Mesero", 2 },
                    { 5, "KITCHEN", null, 2, true, "Cocina", 3 },
                    { 6, "ADMIN", null, 2, true, "Administración", 4 },
                    { 7, "SERVER", null, 2, true, "Servidor", 5 },
                    { 8, "CC", null, 3, true, "Cédula de ciudadanía", 1 },
                    { 9, "CE", null, 3, true, "Cédula de extranjería", 2 },
                    { 10, "NIT", null, 3, true, "Número de identificación tributaria", 3 },
                    { 11, "PASSPORT", null, 3, true, "Pasaporte", 4 },
                    { 12, "PRODUCT", null, 4, true, "Producto", 1 },
                    { 13, "SERVICE", null, 4, true, "Servicio", 2 },
                    { 14, "COMBO", null, 4, true, "Combo", 3 },
                    { 15, "UNIT", null, 5, true, "Unidad", 1 },
                    { 16, "KG", null, 5, true, "Kilogramo", 2 },
                    { 17, "GR", null, 5, true, "Gramo", 3 },
                    { 18, "LITER", null, 5, true, "Litro", 4 },
                    { 19, "ML", null, 5, true, "Mililitro", 5 },
                    { 20, "HOUR", null, 5, true, "Hora", 6 },
                    { 21, "DINE_IN", null, 6, true, "En mesa", 1 },
                    { 22, "TAKEAWAY", null, 6, true, "Para llevar", 2 },
                    { 23, "DELIVERY", null, 6, true, "Domicilio", 3 },
                    { 24, "MANUAL_INCOME", null, 7, true, "Ingreso manual", 1 },
                    { 25, "WITHDRAWAL", null, 7, true, "Retiro", 2 },
                    { 26, "EXPENSE", null, 7, true, "Gasto", 3 },
                    { 27, "POSITIVE_ADJUSTMENT", null, 7, true, "Ajuste positivo", 4 },
                    { 28, "NEGATIVE_ADJUSTMENT", null, 7, true, "Ajuste negativo", 5 },
                    { 29, "INBOUND", null, 8, true, "Entrada", 1 },
                    { 30, "OUTBOUND", null, 8, true, "Salida", 2 },
                    { 31, "ADJUSTMENT", null, 8, true, "Ajuste", 3 },
                    { 32, "SALE", null, 8, true, "Venta", 4 },
                    { 33, "RETURN", null, 8, true, "Devolución", 5 },
                    { 34, "TRANSFER", null, 8, true, "Traslado", 6 },
                    { 35, "NETWORK", null, 9, true, "Red", 1 },
                    { 36, "SYSTEM_QUEUE", null, 9, true, "Cola de impresión del sistema", 2 },
                    { 37, "SALES_RECEIPT", null, 10, true, "Recibo de venta", 1 },
                    { 38, "KITCHEN_TICKET", null, 10, true, "Comanda de cocina", 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_typologies_group_id_code",
                table: "typologies",
                columns: new[] { "group_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "typologies");
        }
    }
}
