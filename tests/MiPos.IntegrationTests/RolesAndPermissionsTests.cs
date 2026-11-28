using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using MiPos.Api.Responses;
using MiPos.Application.Identity.Responses;
using MiPos.Application.Organization.Requests;
using MiPos.Application.Organization.Services;
using MiPos.Domain.Constants.Identity;
using MiPos.Domain.Entities.Identity;
using MiPos.Domain.Entities.Organization;
using MiPos.Infrastructure.Persistence;
using Npgsql;

namespace MiPos.IntegrationTests;

public class RolesAndPermissionsTests(MiPosApiFactory factory) : IClassFixture<MiPosApiFactory>
{
    private const string TestConnectionVariable = "MIPOS_ROLES_TEST_CONNECTION";
    private const string RolesPath = "/api/v1/roles";
    private const string PermissionsPath = "/api/v1/permissions";
    private static readonly string[] InitialCodes =
    [
        "ADMIN_ACCESS", "BILLING_ACCESS", "CASH_REGISTER_MOVEMENTS", "CASH_REGISTER_OPEN_CLOSE",
        "CATALOG_ACCESS", "POS_SELL", "REPORTS_ACCESS", "SANITATION_ACCESS", "SETTINGS_ACCESS",
        "USERS_AND_ROLES_MANAGE"
    ];

    [Theory]
    [InlineData("GET", PermissionsPath)]
    [InlineData("GET", RolesPath)]
    [InlineData("PUT", RolesPath + "/aef636e6-a4d1-4db6-8b3b-2723d658c9bc/permissions")]
    public async Task RoleAndPermissionEndpointsRequireJwt(string method, string path)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        using var response = await client.SendAsync(request);
        await ErrorHandlingTests.AssertApiError(response, 401);
    }

    [Fact]
    public void ModelSeedsTheTenPermissionsAndMatchesTheMigration()
    {
        using var context = new MiPosDbContext(new DbContextOptionsBuilder<MiPosDbContext>().UseNpgsql().Options);
        var model = context.GetService<IDesignTimeModel>().Model;
        var permissions = model.FindEntityType(typeof(Permission))!.GetSeedData().ToList();
        Assert.Equal(InitialCodes, permissions.Select(permission => (string)permission["Code"]!).Order(StringComparer.Ordinal));
        Assert.Equal(10, permissions.Select(permission => (Guid)permission["Id"]!).Distinct().Count());
        Assert.All(permissions, permission =>
        {
            Assert.NotEqual(Guid.Empty, permission["Id"]);
            Assert.False(string.IsNullOrWhiteSpace((string?)permission["Name"]));
            Assert.False(string.IsNullOrWhiteSpace((string?)permission["Description"]));
        });
        Assert.True(model.FindEntityType(typeof(Role))!.FindProperty(nameof(Role.Version))!.IsConcurrencyToken);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [PostgresFact]
    public async Task ProvisionedRolesCanBeQueriedAndTheirPermissionsManagedByBusiness()
    {
        var connection = Environment.GetEnvironmentVariable(TestConnectionVariable)!;
        Assert.StartsWith("mipos_test_", new NpgsqlConnectionStringBuilder(connection).Database);
        var options = new DbContextOptionsBuilder<MiPosDbContext>().UseNpgsql(connection).Options;
        await using var context = new MiPosDbContext(options);
        Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20261125032346_AddBusinessesAndBranches");

        var existing = new Business
        {
            Id = Guid.NewGuid(), Code = "existing", Name = "Negocio existente",
            CountryCode = "CO", CurrencyCode = "COP", TimeZone = "America/Bogota"
        };
        context.Businesses.Add(existing);
        await context.SaveChangesAsync();
        await migrator.MigrateAsync();
        await migrator.MigrateAsync();

        var service = new BusinessService(context, new PasswordHasher<User>());
        var created = await service.CreateBusinessAsync(new CreateBusinessRequest
        {
            Code = "new", Name = "Negocio nuevo",
            CountryCode = "CO", CurrencyCode = "COP", TimeZone = "America/Bogota",
            Administrator = new InitialAdministratorRequest
            {
                Username = "admin", FirstName = "Ana", LastName = "Gómez", Email = "admin@example.com",
                Password = Convert.ToHexString(RandomNumberGenerator.GetBytes(20))
            }
        }, CancellationToken.None);

        var permissions = await context.Permissions.ToDictionaryAsync(permission => permission.Code);
        Assert.Equal(InitialCodes, permissions.Keys.Order(StringComparer.Ordinal));
        var existingCashier = await AssertDefaultRolesAsync(context, existing.Id, permissions);
        var createdCashier = await AssertDefaultRolesAsync(context, created.Id, permissions);
        Assert.Equal(4, await context.Roles.Select(role => role.Id).Distinct().CountAsync());

        var sell = permissions[PermissionCodes.PosSell].Id;
        await AssertAssignmentRejectedAsync(context, created.Id, createdCashier, sell, PostgresErrorCodes.UniqueViolation);
        await AssertAssignmentRejectedAsync(context, existing.Id, createdCashier, sell, PostgresErrorCodes.ForeignKeyViolation);
        await AssertAssignmentRejectedAsync(context, created.Id, createdCashier, Guid.NewGuid(), PostgresErrorCodes.ForeignKeyViolation);

        await context.RolePermissions
            .Where(assignment => assignment.RoleId == existingCashier && assignment.PermissionId == sell)
            .ExecuteDeleteAsync();
        Assert.False(await context.RolePermissions.AnyAsync(assignment =>
            assignment.RoleId == existingCashier && assignment.PermissionId == sell));
        Assert.True(await context.RolePermissions.AnyAsync(assignment =>
            assignment.RoleId == createdCashier && assignment.PermissionId == sell));

        var addedPermission = new Permission { Id = Guid.NewGuid(), Code = "FUTURE", Name = "Permiso futuro" };
        context.Permissions.Add(addedPermission);
        await context.SaveChangesAsync();
        var recipients = await (
            from assignment in context.RolePermissions
            join role in context.Roles on assignment.RoleId equals role.Id
            where assignment.PermissionId == addedPermission.Id
            select role.Code).ToListAsync();
        Assert.Equal(new[] { RoleCodes.Administrator, RoleCodes.Administrator }, recipients);

        using var api = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connection })));
        using var client = api.CreateClient();
        MiPosApiFactory.Authorize(api, client, created.Id);

        using var catalogResponse = await client.GetAsync(PermissionsPath);
        Assert.Equal(HttpStatusCode.OK, catalogResponse.StatusCode);
        var catalog = (await catalogResponse.Content.ReadFromJsonAsync<ApiResponse<List<PermissionResponse>>>())!.Data!;
        Assert.Equal(InitialCodes.Append(addedPermission.Code).Order(), catalog.Select(permission => permission.Code).Order());
        Assert.All(catalog, permission =>
        {
            var stored = permission.Code == addedPermission.Code ? addedPermission : permissions[permission.Code];
            Assert.Equal(stored.Id, permission.Id);
            Assert.Equal(stored.Name, permission.Name);
            Assert.Equal(stored.Description, permission.Description);
        });

        using var rolesResponse = await client.GetAsync(RolesPath);
        Assert.Equal(HttpStatusCode.OK, rolesResponse.StatusCode);
        var roles = (await rolesResponse.Content.ReadFromJsonAsync<ApiResponse<List<RoleResponse>>>())!.Data!;
        Assert.Equal(2, roles.Count);
        var administrator = Assert.Single(roles, role => role.Code == RoleCodes.Administrator);
        var cashier = Assert.Single(roles, role => role.Code == RoleCodes.Cashier);
        Assert.Equal(catalog.Select(permission => permission.Id), administrator.Permissions.Select(permission => permission.Id));
        Assert.Equal(createdCashier, cashier.Id);
        Assert.Equal("Cajero", cashier.Name);
        Assert.True(cashier.IsActive);
        Assert.Equal(new[] { PermissionCodes.CashRegisterOpenClose, PermissionCodes.PosSell },
            cashier.Permissions.Select(permission => permission.Code));

        var path = $"{RolesPath}/{cashier.Id}/permissions";
        var reportPermissionId = permissions[PermissionCodes.ReportsAccess].Id;
        using var updated = await client.PutAsJsonAsync(path, new { permissionIds = new[] { sell, reportPermissionId } });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.True((await updated.Content.ReadFromJsonAsync<ApiResponse<bool>>())!.Data);

        using var missing = await client.PutAsJsonAsync(path, new { permissionIds = new[] { Guid.NewGuid() } });
        await ErrorHandlingTests.AssertApiError(missing, 422);
        using var duplicate = await client.PutAsJsonAsync(path, new { permissionIds = new[] { sell, sell } });
        await ErrorHandlingTests.AssertApiError(duplicate, 422);
        using var omitted = await client.PutAsJsonAsync(path, new { });
        await ErrorHandlingTests.AssertApiError(omitted, 400);
        using var protectedRole = await client.PutAsJsonAsync($"{RolesPath}/{administrator.Id}/permissions",
            new { permissionIds = Array.Empty<Guid>() });
        await ErrorHandlingTests.AssertApiError(protectedRole, 422);
        using var foreignRole = await client.PutAsJsonAsync($"{RolesPath}/{existingCashier}/permissions",
            new { permissionIds = Array.Empty<Guid>() });
        await ErrorHandlingTests.AssertApiError(foreignRole, 404);

        using var updatedRolesResponse = await client.GetAsync(RolesPath);
        Assert.Equal(HttpStatusCode.OK, updatedRolesResponse.StatusCode);
        var updatedRoles = (await updatedRolesResponse.Content.ReadFromJsonAsync<ApiResponse<List<RoleResponse>>>())!.Data!;
        Assert.Equal(new[] { PermissionCodes.PosSell, PermissionCodes.ReportsAccess },
            Assert.Single(updatedRoles, role => role.Id == cashier.Id).Permissions.Select(permission => permission.Code));
        Assert.Equal(2, await context.Roles.Where(role => role.Id == cashier.Id).Select(role => role.Version).SingleAsync());
        Assert.True(await context.RolePermissions.AnyAsync(assignment => assignment.RoleId == existingCashier));
        Assert.Equal(catalog.Count, await context.RolePermissions.CountAsync(assignment => assignment.RoleId == administrator.Id));

        using var cleared = await client.PutAsJsonAsync(path, new { permissionIds = Array.Empty<Guid>() });
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        Assert.False(await context.RolePermissions.AnyAsync(assignment => assignment.RoleId == cashier.Id));
    }

    private static async Task<Guid> AssertDefaultRolesAsync(
        MiPosDbContext context, Guid businessId, Dictionary<string, Permission> permissions)
    {
        var roles = await context.Roles.AsNoTracking().Where(role => role.BusinessId == businessId).ToListAsync();
        Assert.Equal(2, roles.Count);
        var administrator = Assert.Single(roles, role => role.Code == RoleCodes.Administrator);
        var cashier = Assert.Single(roles, role => role.Code == RoleCodes.Cashier);
        Assert.Equal("Administrador", administrator.Name);
        Assert.Equal("Cajero", cashier.Name);
        Assert.All(roles, role =>
        {
            Assert.True(role.IsActive);
            Assert.Null(role.DeletedAt);
            Assert.Equal(1, role.Version);
            Assert.Equal(DateTimeKind.Utc, role.CreatedAt.Kind);
            Assert.Equal(role.CreatedAt, role.UpdatedAt);
        });

        var assignments = await context.RolePermissions.Where(assignment => assignment.BusinessId == businessId).ToListAsync();
        Assert.Equal(permissions.Values.Select(permission => permission.Id).Order(),
            assignments.Where(assignment => assignment.RoleId == administrator.Id).Select(assignment => assignment.PermissionId).Order());
        Assert.Equal(new[] { permissions[PermissionCodes.PosSell].Id, permissions[PermissionCodes.CashRegisterOpenClose].Id }.Order(),
            assignments.Where(assignment => assignment.RoleId == cashier.Id).Select(assignment => assignment.PermissionId).Order());
        return cashier.Id;
    }

    private static async Task AssertAssignmentRejectedAsync(
        MiPosDbContext context, Guid businessId, Guid roleId, Guid permissionId, string expectedError)
    {
        var error = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO role_permissions (business_id, role_id, permission_id)
            VALUES ({businessId}, {roleId}, {permissionId})
            """));
        Assert.Equal(expectedError, error.SqlState);
    }

    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(TestConnectionVariable)))
            {
                Skip = $"Set {TestConnectionVariable} to a disposable empty mipos_test_ database.";
            }
        }
    }
}
