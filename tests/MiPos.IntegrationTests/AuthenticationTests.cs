using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using MiPos.Api.Responses;
using MiPos.Application.Organization.Requests;
using MiPos.Application.Organization.Services;
using MiPos.Domain.Constants.Identity;
using MiPos.Domain.Entities.Identity;
using MiPos.Infrastructure.Persistence;
using Npgsql;

namespace MiPos.IntegrationTests;

public class AuthenticationTests(MiPosApiFactory factory) : IClassFixture<MiPosApiFactory>
{
    private const string ConnectionVariable = "MIPOS_IDENTITY_TEST_CONNECTION";
    private const string LoginPath = "/api/v1/auth/login";

    [Theory]
    [InlineData(null)]
    [InlineData("invalid-token")]
    public async Task ProtectedEndpointsRejectMissingOrInvalidTokens(string? token)
    {
        using var client = factory.CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await client.GetAsync("/api/v1/typologies");
        await ErrorHandlingTests.AssertApiError(response, 401);
        Assert.Equal("Bearer", Assert.Single(response.Headers.WwwAuthenticate).Scheme);
    }

    [PostgresFact]
    public async Task LoginIssuesATokenForTheConfiguredBusinessAndRejectsOtherCredentials()
    {
        var connection = Environment.GetEnvironmentVariable(ConnectionVariable)!;
        Assert.StartsWith("mipos_test_", new NpgsqlConnectionStringBuilder(connection).Database);
        await using var dbContext = new MiPosDbContext(new DbContextOptionsBuilder<MiPosDbContext>().UseNpgsql(connection).Options);
        Assert.True(await dbContext.Database.EnsureCreatedAsync());
        var firstPassword = NewPassword();
        var first = await AddAdministratorAsync(dbContext, "market", firstPassword);
        var otherPassword = NewPassword();
        var other = await AddAdministratorAsync(dbContext, "other", otherPassword);
        using var api = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connection })));
        using var client = api.CreateClient();

        foreach (var (username, password) in new[] { ("admin", NewPassword()), ("missing", firstPassword), ("admin", otherPassword) })
        {
            using var failure = await client.PostAsJsonAsync(LoginPath, new { username, password });
            var error = await ErrorHandlingTests.AssertApiError(failure, 401);
            Assert.Equal("Credenciales inválidas", error.GetProperty("title").GetString());
        }

        using var response = await client.PostAsJsonAsync(LoginPath, new { username = "ADMIN", password = firstPassword });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.Null(envelope!.Error);
        var accessToken = envelope.Data!.AccessToken;
        var token = new JsonWebToken(accessToken);
        Assert.Equal(first.Id.ToString(), token.Subject);
        Assert.Equal(first.BusinessId.ToString(), token.GetClaim("bid").Value);
        Assert.Equal(RoleCodes.Administrator, token.GetClaim("role").Value);
        Assert.Contains(token.Claims, claim => claim.Type == "permission" && claim.Value == PermissionCodes.SettingsAccess);
        Assert.Equal(TimeSpan.FromHours(12), token.ValidTo - token.ValidFrom);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var ownBusiness = await client.GetAsync($"/api/v1/businesses/{first.BusinessId}");
        Assert.Equal(HttpStatusCode.OK, ownBusiness.StatusCode);
        using var foreignBusiness = await client.GetAsync($"/api/v1/businesses/{other.BusinessId}");
        await ErrorHandlingTests.AssertApiError(foreignBusiness, 404);
        using var accepted = await client.GetAsync("/api/v1/typologies");
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        MiPosApiFactory.Authorize(api, client, first.BusinessId);
        using var createdBranch = await client.PostAsJsonAsync($"/api/v1/businesses/{first.BusinessId}/branches",
            new { code = "main", name = "Principal" });
        Assert.Equal(HttpStatusCode.Created, createdBranch.StatusCode);
        using var foreignWrite = await client.PostAsJsonAsync($"/api/v1/businesses/{other.BusinessId}/branches",
            new { code = "main", name = "Principal" });
        await ErrorHandlingTests.AssertApiError(foreignWrite, 404);
    }

    private static async Task<User> AddAdministratorAsync(MiPosDbContext dbContext, string code, string password)
    {
        var business = await new BusinessService(dbContext, new PasswordHasher<User>()).CreateBusinessAsync(new CreateBusinessRequest
        {
            Code = code, Name = code, CountryCode = "CO", CurrencyCode = "COP", TimeZone = "America/Bogota",
            Administrator = new InitialAdministratorRequest { Username = "admin", Name = "Administrador", Password = password }
        }, CancellationToken.None);
        return await dbContext.Users.SingleAsync(user => user.BusinessId == business.Id);
    }

    private static string NewPassword() => Convert.ToHexString(RandomNumberGenerator.GetBytes(20));

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
