using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MiPos.Application.Typologies.Interfaces;
using MiPos.Application.Typologies.Responses;
using MiPos.Domain.Entities.Typologies;
using MiPos.Domain.Enums.Typologies;
using MiPos.Infrastructure.Persistence;

namespace MiPos.IntegrationTests;

public sealed class TypologyTests(MiPosApiFactory factory)
    : IClassFixture<MiPosApiFactory>
{
    [Fact]
    public async Task OpenApiExposesReadOnlyTypologyRoutesWithGroupFilter()
    {
        using var client = factory.CreateClient();
        var openApi = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var paths = openApi.GetProperty("paths");
        var list = paths.GetProperty("/api/v1/typologies");
        var detail = paths.GetProperty("/api/v1/typologies/{typologyId}");
        Assert.Equal("get", Assert.Single(list.EnumerateObject()).Name);
        Assert.Equal("get", Assert.Single(detail.EnumerateObject()).Name);
        var filter = Assert.Single(list.GetProperty("get").GetProperty("parameters").EnumerateArray());
        Assert.Equal("groupId", filter.GetProperty("name").GetString());
        Assert.Equal("query", filter.GetProperty("in").GetString());
        Assert.Contains("integer", filter.GetProperty("schema").GetRawText());
        var typologyId = Assert.Single(detail.GetProperty("get").GetProperty("parameters").EnumerateArray());
        Assert.Equal("typologyId", typologyId.GetProperty("name").GetString());
        Assert.Equal("integer", typologyId.GetProperty("schema").GetProperty("type").GetString());
        AssertResponseEnvelope(openApi, list, "200");
        AssertResponseEnvelope(openApi, detail, "200");
    }

    [Fact]
    public async Task InvalidTypologyIdentifierDoesNotReachTheDatabase()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/typologies/not-an-integer");

        var error = await ErrorHandlingTests.AssertApiError(response, 404);
        Assert.Equal("Recurso no encontrado", error.GetProperty("title").GetString());
    }

    [Fact]
    public async Task TypologyEndpointsBindAndSerializeNumericIdentifiers()
    {
        using var api = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<ITypologyService>();
            services.AddScoped<ITypologyService, TypologyProbeService>();
        }));
        using var client = MiPosApiFactory.CreateAuthenticatedClient(api);

        var list = await client.GetFromJsonAsync<JsonElement>("/api/v1/typologies?groupId=6");
        var detail = await client.GetFromJsonAsync<JsonElement>("/api/v1/typologies/21");
        var row = Assert.Single(list.GetProperty("data").EnumerateArray());
        Assert.Equal(6, row.GetProperty("groupId").GetInt32());
        Assert.Equal(21, detail.GetProperty("data").GetProperty("id").GetInt32());
        Assert.Equal(JsonValueKind.Null, list.GetProperty("error").ValueKind);
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("error").ValueKind);
    }

    [Fact]
    public async Task UnsupportedTypologyMethodReturnsApiErrorAndRetainsAllowHeader()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/api/v1/typologies", null);

        await ErrorHandlingTests.AssertApiError(response, 405);
        Assert.Contains("GET", response.Content.Headers.Allow);
    }

    [Fact]
    public async Task InfrastructureMissingRouteStaysEmpty()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/unknown");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public void RelationalModelMatchesTheDbml()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var table = Assert.Single(model.GetRelationalModel().Tables, table => table.Name == "typologies");
        Assert.Equal("id", Assert.Single(table.PrimaryKey!.Columns).Name);
        var index = Assert.Single(table.Indexes);
        Assert.True(index.IsUnique);
        Assert.Equal(new[] { "group_id", "code" }, index.Columns.Select(column => column.Name));
        Assert.Equal(ValueGenerated.Never, model.FindEntityType(typeof(Typology))!
            .FindProperty(nameof(Typology.Id))!.ValueGenerated);
        Assert.Equal(
            new (string, string, bool)[]
            {
                ("code", "varchar", false),
                ("description", "varchar", true),
                ("group_id", "integer", false),
                ("id", "integer", false),
                ("is_active", "boolean", false),
                ("name", "varchar", false),
                ("sort_order", "integer", false)
            },
            table.Columns.OrderBy(column => column.Name)
                .Select(column => (column.Name, column.StoreType, column.IsNullable)));
        Assert.Equal(0, table.FindColumn("sort_order")!.DefaultValue);
        Assert.Equal(true, table.FindColumn("is_active")!.DefaultValue);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void SeedContainsExactlyTheDocumentedGroupsAndCodes()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var rows = model.FindEntityType(typeof(Typology))!.GetSeedData().ToArray();
        var documented = new (TypologyGroup Group, Type Values, string Codes)[]
        {
            (TypologyGroup.OperationMode, typeof(OperationMode), "CASH_REGISTER_ONLY CASH_REGISTER_AND_WAITERS"),
            (TypologyGroup.DeviceType, typeof(DeviceType), "CASH_REGISTER WAITER KITCHEN ADMIN SERVER"),
            (TypologyGroup.IdentificationType, typeof(IdentificationType), "CC CE NIT PASSPORT"),
            (TypologyGroup.ProductType, typeof(ProductType), "PRODUCT SERVICE COMBO"),
            (TypologyGroup.UnitOfMeasure, typeof(UnitOfMeasure), "UNIT KG GR LITER ML HOUR"),
            (TypologyGroup.OrderType, typeof(OrderType), "DINE_IN TAKEAWAY DELIVERY"),
            (TypologyGroup.CashRegisterMovementType, typeof(CashRegisterMovementType),
                "MANUAL_INCOME WITHDRAWAL EXPENSE POSITIVE_ADJUSTMENT NEGATIVE_ADJUSTMENT"),
            (TypologyGroup.InventoryMovementType, typeof(InventoryMovementType), "INBOUND OUTBOUND ADJUSTMENT SALE RETURN TRANSFER"),
            (TypologyGroup.PrinterConnectionType, typeof(PrinterConnectionType), "NETWORK SYSTEM_QUEUE"),
            (TypologyGroup.PrintDocumentType, typeof(PrintDocumentType), "SALES_RECEIPT KITCHEN_TICKET")
        };

        Assert.Equal(Enumerable.Range(1, 10), documented.Select(item => (int)item.Group));
        Assert.Equal(documented.Select(item => item.Group), Enum.GetValues<TypologyGroup>());
        Assert.Equal(Enumerable.Range(1, 38), rows.Select(row => (int)row[nameof(Typology.Id)]!).Order());
        var nextId = 1;
        foreach (var (group, values, codes) in documented)
        {
            var groupRows = rows.Where(row => (int)row[nameof(Typology.GroupId)]! == (int)group)
                .OrderBy(row => (int)row[nameof(Typology.Id)]!).ToArray();
            var expectedCodes = codes.Split(' ');
            var expectedIds = Enumerable.Range(nextId, expectedCodes.Length);

            Assert.Equal(expectedCodes, groupRows.Select(row => (string)row[nameof(Typology.Code)]!));
            Assert.Equal(expectedIds, Enum.GetValuesAsUnderlyingType(values).Cast<int>());
            Assert.Equal(expectedIds, groupRows.Select(row => (int)row[nameof(Typology.Id)]!));
            Assert.Equal(Enumerable.Range(1, groupRows.Length),
                groupRows.Select(row => (int)row[nameof(Typology.SortOrder)]!));
            Assert.All(groupRows, row =>
            {
                Assert.True((bool)row[nameof(Typology.IsActive)]!);
                Assert.NotEqual(row[nameof(Typology.Code)], row[nameof(Typology.Name)]);
            });
            nextId += expectedCodes.Length;
        }
    }

    private sealed class TypologyProbeService : ITypologyService
    {
        public Task<List<TypologyResponse>> GetTypologiesAsync(int? groupId, CancellationToken cancellationToken) =>
            Task.FromResult(groupId == 6 ? new List<TypologyResponse> { CreateResponse(21) } : []);

        public Task<TypologyResponse> GetTypologyByIdAsync(int typologyId, CancellationToken cancellationToken) =>
            Task.FromResult(CreateResponse(typologyId));

        private static TypologyResponse CreateResponse(int id) =>
            new() { Id = id, GroupId = 6, Code = "DINE_IN", Name = "En mesa", SortOrder = 1, IsActive = true };
    }

    private static MiPosDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MiPosDbContext>().UseNpgsql().Options);

    private static void AssertResponseEnvelope(JsonElement openApi, JsonElement path, string status)
    {
        var schema = path.GetProperty("get").GetProperty("responses").GetProperty(status)
            .GetProperty("content").GetProperty("application/json").GetProperty("schema");
        if (schema.TryGetProperty("$ref", out var reference))
        {
            var name = reference.GetString()!.Split('/')[^1];
            schema = openApi.GetProperty("components").GetProperty("schemas").GetProperty(name);
        }

        Assert.Equal(new[] { "data", "error" },
            schema.GetProperty("properties").EnumerateObject().Select(property => property.Name).Order());
    }
}
