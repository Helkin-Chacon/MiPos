using MiPos.Domain.Constants.Identity;
using MiPos.Domain.Entities.Identity;

namespace MiPos.Infrastructure.Persistence.Seed;

internal static class PermissionSeed
{
    internal static readonly Permission[] Values =
    [
        new Permission
        {
            Id = new Guid("e2aa09a2-9a8d-42fd-827c-5069cb54d3d2"),
            Code = PermissionCodes.PosSell,
            Name = "Vender en caja",
            Description = "Gestión de mesas, pedidos, ventas y cobro."
        },
        new Permission
        {
            Id = new Guid("2afadaf0-33f4-4623-b734-c4e8d917dbd2"),
            Code = PermissionCodes.CashRegisterOpenClose,
            Name = "Abrir y cerrar caja",
            Description = "Apertura y cierre de caja."
        },
        new Permission
        {
            Id = new Guid("7b94d819-6dfc-46f2-8298-9866761de324"),
            Code = PermissionCodes.CashRegisterMovements,
            Name = "Movimientos de caja",
            Description = "Registro de ingresos y salidas de caja."
        },
        new Permission
        {
            Id = new Guid("ce909594-b90c-4804-a9c7-34a32f952387"),
            Code = PermissionCodes.AdminAccess,
            Name = "Acceso al administrador",
            Description = "Acceso al panel administrativo."
        },
        new Permission
        {
            Id = new Guid("ab044f9f-e469-4b36-993d-a161577de166"),
            Code = PermissionCodes.CatalogAccess,
            Name = "Catálogo",
            Description = "Gestión de categorías, productos y combos."
        },
        new Permission
        {
            Id = new Guid("628405a6-f077-4153-af03-893f17fc8ea4"),
            Code = PermissionCodes.ReportsAccess,
            Name = "Reportes",
            Description = "Acceso a todos los reportes."
        },
        new Permission
        {
            Id = new Guid("ce203714-a5e9-47cb-bfb6-8b545844d874"),
            Code = PermissionCodes.SettingsAccess,
            Name = "Configuración",
            Description = "Configuración del negocio, operación del POS, métodos de pago e impresión."
        },
        new Permission
        {
            Id = new Guid("e8e229be-d2c9-44ac-acbe-accf4e8db0a5"),
            Code = PermissionCodes.UsersAndRolesManage,
            Name = "Usuarios y permisos",
            Description = "Administración de usuarios, roles y permisos."
        },
        new Permission
        {
            Id = new Guid("2e8e8006-ac21-400e-aabd-56121c21adda"),
            Code = PermissionCodes.SanitationAccess,
            Name = "Sanidad",
            Description = "Acceso al módulo de sanidad."
        },
        new Permission
        {
            Id = new Guid("49055b32-382d-4697-b53f-aff65296184e"),
            Code = PermissionCodes.BillingAccess,
            Name = "Facturación electrónica",
            Description = "Acceso a todos los apartados de facturación electrónica."
        },
    ];
}
