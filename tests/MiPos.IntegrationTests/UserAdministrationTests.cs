using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiPos.Api.Responses;
using MiPos.Application.Identity.Responses;
using MiPos.Application.Organization.Requests;
using MiPos.Application.Organization.Services;
using MiPos.Domain.Constants.Identity;
using MiPos.Domain.Entities.Identity;
using MiPos.Infrastructure.Persistence;
using Npgsql;

namespace MiPos.IntegrationTests;

public class UserAdministrationTests(MiPosApiFactory factory) : IClassFixture<MiPosApiFactory>
{
    private const string ConnectionVariable = "MIPOS_USERS_TEST_CONNECTION";
    private const string UsersPath = "/api/v1/users";
    private const string ExampleUserPath = UsersPath + "/3146b93f-00e7-47dc-b505-acddb331dbd2";

    [Theory]
    [InlineData("GET", UsersPath)]
    [InlineData("POST", UsersPath)]
    [InlineData("PUT", ExampleUserPath)]
    [InlineData("PATCH", ExampleUserPath + "/status")]
    public async Task AdministrationRejectsMissingOrInvalidJwt(string method, string path)
    {
        using var anonymous = factory.CreateClient();
        using var anonymousRequest = new HttpRequestMessage(new HttpMethod(method), path);
        using var invalidRequest = new HttpRequestMessage(new HttpMethod(method), path);
        invalidRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        using var unauthorized = await anonymous.SendAsync(anonymousRequest);
        using var invalid = await anonymous.SendAsync(invalidRequest);
        await ErrorHandlingTests.AssertApiError(unauthorized, 401);
        await ErrorHandlingTests.AssertApiError(invalid, 401);
    }

    [PostgresFact]
    public async Task AuthenticatedUsersManageOnlyTheirOwnBusinessUsers()
    {
        var connection = Environment.GetEnvironmentVariable(ConnectionVariable)!;
        Assert.StartsWith("mipos_test_", new NpgsqlConnectionStringBuilder(connection).Database);
        var options = new DbContextOptionsBuilder<MiPosDbContext>().UseNpgsql(connection).Options;
        await using var database = new MiPosDbContext(options);
        Assert.True(await database.Database.EnsureCreatedAsync());
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(20));
        var owner = await AddBusinessAsync(database, "own", password);
        var foreign = await AddBusinessAsync(database, "foreign", password);
        var cashierRole = await database.Roles.SingleAsync(role =>
            role.BusinessId == owner.BusinessId && role.Code == RoleCodes.Cashier);

        using var api = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connection })));
        using var client = api.CreateClient();
        using var employeeClient = api.CreateClient();
        await AuthorizeAsync(client, owner.Username, password);

        Assert.Equal(owner.Id, Assert.Single(await ListUsersAsync(client)).Id);
        using var foreignRole = await client.PostAsJsonAsync(UsersPath, new
        {
            username = "employee", password, firstName = "Ana", lastName = "Ruiz", email = "ana@example.com",
            roleId = foreign.RoleId
        });
        await ErrorHandlingTests.AssertApiError(foreignRole, 422);

        using var created = await client.PostAsJsonAsync(UsersPath, new
        {
            username = " Employee ", password, firstName = "Ana", lastName = "Ruiz", email = "ana@example.com",
            roleId = cashierRole.Id, businessId = foreign.BusinessId, isActive = false
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var employee = (await created.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>())!.Data!;
        Assert.Equal("Employee", employee.Username);
        Assert.Equal(RoleCodes.Cashier, employee.RoleCode);
        Assert.True(employee.IsActive);
        var employeePath = $"{UsersPath}/{employee.Id}";
        await AuthorizeAsync(employeeClient, "EMPLOYEE", password);
        Assert.Equal(new[] { owner.Id, employee.Id }.Order(),
            (await ListUsersAsync(employeeClient)).Select(user => user.Id).Order());

        using var duplicate = await client.PostAsJsonAsync(UsersPath, new
        {
            username = $" {foreign.Username.ToUpperInvariant()} ", password, firstName = "Ana", lastName = "Ruiz",
            email = "ana@example.com", roleId = cashierRole.Id
        });
        await ErrorHandlingTests.AssertApiError(duplicate, 409);
        Assert.Equal(new[] { owner.Id, employee.Id }.Order(), (await ListUsersAsync(client)).Select(user => user.Id).Order());

        var profile = new
        {
            username = "supervisor", firstName = "María", lastName = "López", email = "maria@example.com",
            roleId = owner.RoleId, businessId = foreign.BusinessId, isActive = false, password = "ignored"
        };
        using var foreignUpdate = await client.PutAsJsonAsync($"{UsersPath}/{foreign.Id}", profile);
        await ErrorHandlingTests.AssertApiError(foreignUpdate, 404);
        using var foreignStatus = await client.PatchAsJsonAsync($"{UsersPath}/{foreign.Id}/status", new { isActive = false });
        await ErrorHandlingTests.AssertApiError(foreignStatus, 404);

        var ownerStatusPath = $"{UsersPath}/{owner.Id}/status";
        using var lastAdministrator = await client.PatchAsJsonAsync(ownerStatusPath, new { isActive = false });
        var conflict = await ErrorHandlingTests.AssertApiError(lastAdministrator, 409);
        Assert.Equal("Último administrador activo", conflict.GetProperty("title").GetString());
        using var lastAdministratorRole = await client.PutAsJsonAsync($"{UsersPath}/{owner.Id}", new
        {
            owner.Username, owner.FirstName, owner.LastName, owner.Email, roleId = cashierRole.Id
        });
        await ErrorHandlingTests.AssertApiError(lastAdministratorRole, 409);

        using var updated = await client.PutAsJsonAsync(employeePath, profile);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedEmployee = Assert.Single(await ListUsersAsync(client), user => user.Id == employee.Id);
        Assert.Equal("supervisor", updatedEmployee.Username);
        Assert.Equal("María", updatedEmployee.FirstName);
        Assert.Equal("López", updatedEmployee.LastName);
        Assert.Equal("maria@example.com", updatedEmployee.Email);
        Assert.Equal(owner.RoleId, updatedEmployee.RoleId);
        Assert.Equal(RoleCodes.Administrator, updatedEmployee.RoleCode);
        Assert.True(updatedEmployee.IsActive);
        await AuthorizeAsync(employeeClient, updatedEmployee.Username, password);

        using var selfDeactivation = await client.PatchAsJsonAsync(ownerStatusPath, new { isActive = false });
        await ErrorHandlingTests.AssertApiError(selfDeactivation, 422);
        using var missingStatus = await client.PatchAsJsonAsync(employeePath + "/status", new { });
        await ErrorHandlingTests.AssertApiError(missingStatus, 400);
        using var deactivated = await client.PatchAsJsonAsync(employeePath + "/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        Assert.False(Assert.Single(await ListUsersAsync(client), user => user.Id == employee.Id).IsActive);
        Assert.False(Assert.Single(await ListUsersAsync(employeeClient), user => user.Id == employee.Id).IsActive);
        using var disabledLogin = await employeeClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username = updatedEmployee.Username, password });
        await ErrorHandlingTests.AssertApiError(disabledLogin, 401);

        using var activated = await client.PatchAsJsonAsync(employeePath + "/status", new { isActive = true });
        Assert.Equal(HttpStatusCode.OK, activated.StatusCode);
        Assert.True(Assert.Single(await ListUsersAsync(client), user => user.Id == employee.Id).IsActive);
        await AuthorizeAsync(employeeClient, updatedEmployee.Username, password);

        var demotions = await Task.WhenAll(
            client.PutAsJsonAsync($"{UsersPath}/{owner.Id}", new
            {
                owner.Username, owner.FirstName, owner.LastName, owner.Email, roleId = cashierRole.Id
            }),
            employeeClient.PutAsJsonAsync(employeePath, new
            {
                updatedEmployee.Username, updatedEmployee.FirstName, updatedEmployee.LastName, updatedEmployee.Email,
                roleId = cashierRole.Id
            }));
        using var ownerDemotion = demotions[0];
        using var employeeDemotion = demotions[1];
        Assert.Single(demotions, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(demotions, response => response.StatusCode == HttpStatusCode.Conflict);
        var remainingAdministrator = ownerDemotion.IsSuccessStatusCode ? employeeClient : client;
        Assert.Single(await ListUsersAsync(remainingAdministrator), user =>
            user.IsActive && user.RoleCode == RoleCodes.Administrator);
    }

    private static async Task<User> AddBusinessAsync(MiPosDbContext database, string code, string password)
    {
        var business = await new BusinessService(database, new PasswordHasher<User>()).CreateBusinessAsync(new CreateBusinessRequest
        {
            Code = code, Name = code, CountryCode = "CO", CurrencyCode = "COP", TimeZone = "America/Bogota",
            Administrator = new InitialAdministratorRequest
            {
                Username = $"admin-{code}", FirstName = "Ana", LastName = "Ruiz", Email = $"admin-{code}@example.com", Password = password
            }
        }, CancellationToken.None);
        return await database.Users.SingleAsync(user => user.BusinessId == business.Id);
    }

    private static async Task AuthorizeAsync(HttpClient client, string username, string password)
    {
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var envelope = await login.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", envelope!.Data!.AccessToken);
    }

    private static async Task<List<UserResponse>> ListUsersAsync(HttpClient client)
    {
        using var response = await client.GetAsync(UsersPath);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<List<UserResponse>>>())!.Data!;
    }

    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable)))
            {
                Skip = $"Set {ConnectionVariable} to a disposable empty mipos_test_ database.";
            }
        }
    }
}
