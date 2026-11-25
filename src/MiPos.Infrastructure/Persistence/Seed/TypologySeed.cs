using MiPos.Domain.Entities.Typologies;
using MiPos.Domain.Enums.Typologies;

namespace MiPos.Infrastructure.Persistence.Seed;

internal static class TypologySeed
{
    public static readonly Typology[] Values =
    [
        Create((int)OperationMode.CashRegisterOnly, TypologyGroup.OperationMode, "CASH_REGISTER_ONLY", "Solo caja", 1),
        Create((int)OperationMode.CashRegisterAndWaiters, TypologyGroup.OperationMode, "CASH_REGISTER_AND_WAITERS", "Caja y meseros", 2),

        Create((int)DeviceType.CashRegister, TypologyGroup.DeviceType, "CASH_REGISTER", "Caja", 1),
        Create((int)DeviceType.Waiter, TypologyGroup.DeviceType, "WAITER", "Mesero", 2),
        Create((int)DeviceType.Kitchen, TypologyGroup.DeviceType, "KITCHEN", "Cocina", 3),
        Create((int)DeviceType.Admin, TypologyGroup.DeviceType, "ADMIN", "Administración", 4),
        Create((int)DeviceType.Server, TypologyGroup.DeviceType, "SERVER", "Servidor", 5),

        Create((int)IdentificationType.Cc, TypologyGroup.IdentificationType, "CC", "Cédula de ciudadanía", 1),
        Create((int)IdentificationType.Ce, TypologyGroup.IdentificationType, "CE", "Cédula de extranjería", 2),
        Create((int)IdentificationType.Nit, TypologyGroup.IdentificationType, "NIT", "Número de identificación tributaria", 3),
        Create((int)IdentificationType.Passport, TypologyGroup.IdentificationType, "PASSPORT", "Pasaporte", 4),

        Create((int)ProductType.Product, TypologyGroup.ProductType, "PRODUCT", "Producto", 1),
        Create((int)ProductType.Service, TypologyGroup.ProductType, "SERVICE", "Servicio", 2),
        Create((int)ProductType.Combo, TypologyGroup.ProductType, "COMBO", "Combo", 3),

        Create((int)UnitOfMeasure.Unit, TypologyGroup.UnitOfMeasure, "UNIT", "Unidad", 1),
        Create((int)UnitOfMeasure.Kg, TypologyGroup.UnitOfMeasure, "KG", "Kilogramo", 2),
        Create((int)UnitOfMeasure.Gr, TypologyGroup.UnitOfMeasure, "GR", "Gramo", 3),
        Create((int)UnitOfMeasure.Liter, TypologyGroup.UnitOfMeasure, "LITER", "Litro", 4),
        Create((int)UnitOfMeasure.Ml, TypologyGroup.UnitOfMeasure, "ML", "Mililitro", 5),
        Create((int)UnitOfMeasure.Hour, TypologyGroup.UnitOfMeasure, "HOUR", "Hora", 6),

        Create((int)OrderType.DineIn, TypologyGroup.OrderType, "DINE_IN", "En mesa", 1),
        Create((int)OrderType.Takeaway, TypologyGroup.OrderType, "TAKEAWAY", "Para llevar", 2),
        Create((int)OrderType.Delivery, TypologyGroup.OrderType, "DELIVERY", "Domicilio", 3),

        Create((int)CashRegisterMovementType.ManualIncome, TypologyGroup.CashRegisterMovementType, "MANUAL_INCOME", "Ingreso manual", 1),
        Create((int)CashRegisterMovementType.Withdrawal, TypologyGroup.CashRegisterMovementType, "WITHDRAWAL", "Retiro", 2),
        Create((int)CashRegisterMovementType.Expense, TypologyGroup.CashRegisterMovementType, "EXPENSE", "Gasto", 3),
        Create((int)CashRegisterMovementType.PositiveAdjustment, TypologyGroup.CashRegisterMovementType, "POSITIVE_ADJUSTMENT", "Ajuste positivo", 4),
        Create((int)CashRegisterMovementType.NegativeAdjustment, TypologyGroup.CashRegisterMovementType, "NEGATIVE_ADJUSTMENT", "Ajuste negativo", 5),

        Create((int)InventoryMovementType.Inbound, TypologyGroup.InventoryMovementType, "INBOUND", "Entrada", 1),
        Create((int)InventoryMovementType.Outbound, TypologyGroup.InventoryMovementType, "OUTBOUND", "Salida", 2),
        Create((int)InventoryMovementType.Adjustment, TypologyGroup.InventoryMovementType, "ADJUSTMENT", "Ajuste", 3),
        Create((int)InventoryMovementType.Sale, TypologyGroup.InventoryMovementType, "SALE", "Venta", 4),
        Create((int)InventoryMovementType.Return, TypologyGroup.InventoryMovementType, "RETURN", "Devolución", 5),
        Create((int)InventoryMovementType.Transfer, TypologyGroup.InventoryMovementType, "TRANSFER", "Traslado", 6),

        Create((int)PrinterConnectionType.Network, TypologyGroup.PrinterConnectionType, "NETWORK", "Red", 1),
        Create((int)PrinterConnectionType.SystemQueue, TypologyGroup.PrinterConnectionType, "SYSTEM_QUEUE", "Cola de impresión del sistema", 2),

        Create((int)PrintDocumentType.SalesReceipt, TypologyGroup.PrintDocumentType, "SALES_RECEIPT", "Recibo de venta", 1),
        Create((int)PrintDocumentType.KitchenTicket, TypologyGroup.PrintDocumentType, "KITCHEN_TICKET", "Comanda de cocina", 2),
    ];

    private static Typology Create(int id, TypologyGroup group, string code, string name, int sortOrder) => new()
    {
        Id = id,
        GroupId = (int)group,
        Code = code,
        Name = name,
        SortOrder = sortOrder,
        IsActive = true
    };
}
