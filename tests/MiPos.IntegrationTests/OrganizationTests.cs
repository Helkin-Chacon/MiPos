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

public sealed class OrganizationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestConnectionVariable = "MIPOS_ORGANIZATION_TEST_CONNECTION";
    private const string BusinessesPath = "/api/v1/businesses";

    [Fact]
    public async Task InvalidRequestsKeepTheValidationEnvelope()
    {
        using var client = factory.CreateClient();
        using var invalidCountry = await client.PostAsJsonAsync(BusinessesPath, NewBusiness(countryCode: "COL"));
        await ErrorHandlingTests.AssertApiError(invalidCountry, 400);

        using var missingName = await client.PostAsJsonAsync($"{BusinessesPath}/{Guid.NewGuid()}/branches", new { code = "main" });
        await ErrorHandlingTests.AssertApiError(missingName, 400);
    }

    [Theory]
    [InlineData("GET", BusinessesPath)]
    [InlineData("PUT", BusinessesPath + "/00000000-0000-0000-0000-000000000001")]
    [InlineData("PUT", "/api/v1/branches/00000000-0000-0000-0000-000000000001")]
    public async Task UnneededAdministrationMethodsAreUnavailable(string method, string path)
    {
        using var client = factory.CreateClient();
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
        using var client = api.CreateClient();

        var business = await CreateAsync<BusinessResponse>(client, BusinessesPath, NewBusiness(), BusinessesPath);
        Assert.True(business.IsActive);
        Assert.Equal(1, business.Version);
        Assert.Equal(DateTimeKind.Utc, business.CreatedAt.Kind);
        Assert.Equal(business.CreatedAt, business.UpdatedAt);
        using var duplicateBusiness = await client.PostAsJsonAsync(BusinessesPath, NewBusiness());
        await ErrorHandlingTests.AssertApiError(duplicateBusiness, 409);

        var inactive = await CreateAsync<BusinessResponse>(client, BusinessesPath,
            NewBusiness(code: "inactive", isActive: false), BusinessesPath);
        Assert.False(inactive.IsActive);
        using var inactiveBranch = await client.PostAsJsonAsync($"{BusinessesPath}/{inactive.Id}/branches", NewBranch());
        await ErrorHandlingTests.AssertApiError(inactiveBranch, 409);
        using var missingBusiness = await client.PostAsJsonAsync($"{BusinessesPath}/{Guid.NewGuid()}/branches", NewBranch());
        await ErrorHandlingTests.AssertApiError(missingBusiness, 404);

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

        var other = await CreateAsync<BusinessResponse>(client, BusinessesPath, NewBusiness(code: "other"), BusinessesPath);
        var explicitTimeZone = await CreateAsync<BranchResponse>(client, $"{BusinessesPath}/{other.Id}/branches",
            NewBranch(timeZone: "UTC"), branchDetailsPath);
        Assert.Equal("UTC", explicitTimeZone.TimeZone);
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
            IsActive = isActive
        };

    private static CreateBranchRequest NewBranch(int? operationModeId = null, string? timeZone = null) =>
        new()
        {
            Code = "main",
            Name = "Principal",
            OperationModeId = operationModeId,
            TimeZone = timeZone
        };

    private static async Task<T> CreateAsync<T>(HttpClient client, string path, object request, string detailsPath)
    {
        using var response = await client.PostAsJsonAsync(path, request);
        var data = await ReadAsync<T>(response, HttpStatusCode.Created);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith($"{detailsPath}/", response.Headers.Location.AbsolutePath);
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
