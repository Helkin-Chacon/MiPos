using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using MiPos.Domain.Constants.Identity;
using MiPos.Domain.Entities.Identity;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using MiPos.Api.Responses;
using MiPos.Application.Organization.Requests;
using MiPos.Application.Organization.Responses;
using MiPos.Domain.Enums.Typologies;
using MiPos.Infrastructure.Persistence;
using Npgsql;

namespace MiPos.IntegrationTests;

public sealed class OrganizationTests(MiPosApiFactory factory)
    : IClassFixture<MiPosApiFactory>
{
    private const string TestConnectionVariable = "MIPOS_ORGANIZATION_TEST_CONNECTION";
    private const string BusinessesPath = "/api/v1/businesses";

    [Fact]
    public async Task InvalidRequestsKeepTheValidationEnvelope()
    {
        using var client = MiPosApiFactory.CreateAuthenticatedClient(factory);
        using var invalidCountry = await client.PostAsJsonAsync(BusinessesPath, NewBusiness(countryCode: "COL"));
        await ErrorHandlingTests.AssertApiError(invalidCountry, 400);

        var missingAdministrator = NewBusiness();
        missingAdministrator.Administrator = null!;
        using var missingAdministratorResponse = await client.PostAsJsonAsync(BusinessesPath, missingAdministrator);
        await ErrorHandlingTests.AssertApiError(missingAdministratorResponse, 400);

        using var missingName = await client.PostAsJsonAsync($"{BusinessesPath}/{Guid.NewGuid()}/branches", new { code = "main" });
        await ErrorHandlingTests.AssertApiError(missingName, 400);
    }

    [Theory]
    [InlineData("GET", BusinessesPath)]
    [InlineData("PUT", BusinessesPath + "/00000000-0000-0000-0000-000000000001")]
    [InlineData("PUT", "/api/v1/branches/00000000-0000-0000-0000-000000000001")]
    public async Task UnneededAdministrationMethodsAreUnavailable(string method, string path)
    {
        using var client = MiPosApiFactory.CreateAuthenticatedClient(factory);
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        using var response = await client.SendAsync(request);
        await ErrorHandlingTests.AssertApiError(response, 405);
    }

    [Fact]
    public void OrganizationModelContainsOnlyTheSpecifiedIndexesAndRelationships()
    {
        using var context = new MiPosDbContext(new DbContextOptionsBuilder<MiPosDbContext>().UseNpgsql().Options);
        var model = context.GetService<IDesignTimeModel>().Model.GetRelationalModel();
        var businesses = model.FindTable("businesses", null)!;
        var branches = model.FindTable("branches", null)!;

        var businessCode = Assert.Single(businesses.Indexes);
        Assert.True(businessCode.IsUnique);
        Assert.Equal("code", Assert.Single(businessCode.Columns).Name);
        Assert.Equal(2, branches.Indexes.Count());
        Assert.All(branches.Indexes, index => Assert.True(index.IsUnique));
        Assert.Contains(branches.Indexes, index => index.Columns.Select(column => column.Name).SequenceEqual(["id", "business_id"]));
        Assert.Contains(branches.Indexes, index => index.Columns.Select(column => column.Name).SequenceEqual(["business_id", "code"]));
        Assert.Equal(new[] { "businesses", "typologies" },
            branches.ForeignKeyConstraints.Select(key => key.PrincipalTable.Name).Order());
        Assert.Equal("char(2)", businesses.FindColumn("country_code")!.StoreType);
        Assert.Equal("char(3)", businesses.FindColumn("currency_code")!.StoreType);
        Assert.Equal("timestamp with time zone", branches.FindColumn("created_at")!.StoreType);
        Assert.Equal(1L, branches.FindColumn("version")!.DefaultValue);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [PostgresFact]
    public async Task OrganizationEndpointsPersistRulesAndVersionsInPostgres()
    {
        var connection = Environment.GetEnvironmentVariable(TestConnectionVariable)!;
        var database = new NpgsqlConnectionStringBuilder(connection).Database;
        Assert.StartsWith("mipos_test_", database);
        var options = new DbContextOptionsBuilder<MiPosDbContext>().UseNpgsql(connection).Options;
        await using var setup = new MiPosDbContext(options);
        Assert.True(await setup.Database.EnsureCreatedAsync());

        using var api = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connection })));
        using var client = MiPosApiFactory.CreateAuthenticatedClient(api);

        var request = NewBusiness();
        var business = await CreateAsync<BusinessResponse>(client, BusinessesPath, request, BusinessesPath, api);
        var administrator = await setup.Users.SingleAsync(user => user.BusinessId == business.Id);
        var administratorRole = await setup.Roles.SingleAsync(role => role.Id == administrator.RoleId);
        Assert.Equal(RoleCodes.Administrator, administratorRole.Code);
        Assert.Equal(business.Id, administratorRole.BusinessId);
        Assert.Equal("ADMIN", administrator.NormalizedUsername);
        Assert.Equal(request.Administrator.Name, administrator.Name);
        Assert.NotEqual(request.Administrator.Password, administrator.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(administrator, administrator.PasswordHash, request.Administrator.Password));
        Assert.True(business.IsActive);
        Assert.Equal(1, business.Version);
        Assert.Equal(DateTimeKind.Utc, business.CreatedAt.Kind);
        Assert.Equal(business.CreatedAt, business.UpdatedAt);
        using var duplicateBusiness = await client.PostAsJsonAsync(BusinessesPath, NewBusiness());
        await ErrorHandlingTests.AssertApiError(duplicateBusiness, 409);
        Assert.Equal(1, await setup.Businesses.CountAsync());
        Assert.Equal(2, await setup.Roles.CountAsync());
        Assert.Equal(1, await setup.Users.CountAsync());

        var inactive = await CreateAsync<BusinessResponse>(client, BusinessesPath,
            NewBusiness(code: "inactive", isActive: false), BusinessesPath, api);
        Assert.False(inactive.IsActive);
        using var inactiveBranch = await client.PostAsJsonAsync($"{BusinessesPath}/{inactive.Id}/branches", NewBranch());
        await ErrorHandlingTests.AssertApiError(inactiveBranch, 409);
        using var missingBusiness = await client.PostAsJsonAsync($"{BusinessesPath}/{Guid.NewGuid()}/branches", NewBranch());
        await ErrorHandlingTests.AssertApiError(missingBusiness, 404);

        MiPosApiFactory.Authorize(api, client, business.Id);
        var branchesPath = $"{BusinessesPath}/{business.Id}/branches";
        const string branchDetailsPath = "/api/v1/branches";
        var branch = await CreateAsync<BranchResponse>(client, branchesPath, NewBranch(), branchDetailsPath);
        Assert.Equal(business.Id, branch.BusinessId);
        Assert.Equal((int)OperationMode.CashRegisterOnly, branch.OperationModeId);
        Assert.Equal(business.TimeZone, branch.TimeZone);
        Assert.False(branch.UsesTables);
        Assert.False(branch.InventoryEnabled);
        Assert.True(branch.IsActive);
        Assert.Equal(1, branch.Version);
        using var duplicateBranch = await client.PostAsJsonAsync(branchesPath, NewBranch());
        await ErrorHandlingTests.AssertApiError(duplicateBranch, 409);
        using var wrongMode = await client.PostAsJsonAsync(branchesPath,
            NewBranch(operationModeId: (int)DeviceType.CashRegister));
        await ErrorHandlingTests.AssertApiError(wrongMode, 422);
        await setup.Typologies.Where(typology => typology.Id == (int)OperationMode.CashRegisterAndWaiters)
            .ExecuteUpdateAsync(typologies => typologies.SetProperty(typology => typology.IsActive, false));
        using var inactiveMode = await client.PostAsJsonAsync(branchesPath,
            NewBranch(operationModeId: (int)OperationMode.CashRegisterAndWaiters));
        await ErrorHandlingTests.AssertApiError(inactiveMode, 422);

        var other = await CreateAsync<BusinessResponse>(client, BusinessesPath, NewBusiness(code: "other"), BusinessesPath, api);
        var explicitTimeZone = await CreateAsync<BranchResponse>(client, $"{BusinessesPath}/{other.Id}/branches",
            NewBranch(timeZone: "UTC"), branchDetailsPath);
        Assert.Equal("UTC", explicitTimeZone.TimeZone);
        MiPosApiFactory.Authorize(api, client, business.Id);
        using var foreignBranch = await client.GetAsync($"{branchDetailsPath}/{explicitTimeZone.Id}");
        await ErrorHandlingTests.AssertApiError(foreignBranch, 404);
        var branches = await client.GetFromJsonAsync<ApiResponse<List<BranchResponse>>>(branchesPath);
        Assert.Equal(branch.Id, Assert.Single(branches!.Data!).Id);

        await using var concurrent = new MiPosDbContext(options);
        var current = await setup.Branches.SingleAsync(entity => entity.Id == branch.Id);
        var stale = await concurrent.Branches.SingleAsync(entity => entity.Id == branch.Id);
        current.Version++;
        stale.Version++;
        await setup.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => concurrent.SaveChangesAsync());
    }

    private static CreateBusinessRequest NewBusiness(string code = "market", bool isActive = true, string countryCode = "CO") =>
        new()
        {
            Code = code,
            Name = "Mercado",
            CountryCode = countryCode,
            CurrencyCode = "COP",
            TimeZone = "America/Bogota",
            IsActive = isActive,
            Administrator = new InitialAdministratorRequest
            {
                Username = "admin", Name = "Administrador",
                Password = Convert.ToHexString(RandomNumberGenerator.GetBytes(20))
            }
        };

    private static CreateBranchRequest NewBranch(int? operationModeId = null, string? timeZone = null) =>
        new()
        {
            Code = "main",
            Name = "Principal",
            OperationModeId = operationModeId,
            TimeZone = timeZone
        };

    private static async Task<T> CreateAsync<T>(HttpClient client, string path, object request, string detailsPath,
        WebApplicationFactory<Program>? api = null)
    {
        using var response = await client.PostAsJsonAsync(path, request);
        var data = await ReadAsync<T>(response, HttpStatusCode.Created);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith($"{detailsPath}/", response.Headers.Location.AbsolutePath);
        if (api is not null && data is BusinessResponse business)
        {
            MiPosApiFactory.Authorize(api, client, business.Id);
        }

        using var detail = await client.GetAsync(response.Headers.Location);
        await ReadAsync<T>(detail, HttpStatusCode.OK);
        return data;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        Assert.Null(payload!.Error);
        return Assert.IsType<T>(payload.Data);
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
