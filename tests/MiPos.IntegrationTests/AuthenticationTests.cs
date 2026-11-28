using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
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
    private const string ProfilePath = "/api/v1/users/me";
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

    [Theory]
    [InlineData("GET", ProfilePath)]
    [InlineData("PUT", ProfilePath)]
    [InlineData("PUT", ProfilePath + "/password")]
    public async Task CurrentUserEndpointsRequireJwt(string method, string path)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        using var response = await client.SendAsync(request);
        await ErrorHandlingTests.AssertApiError(response, 401);
    }

    [Theory]
    [InlineData("username", "")]
    [InlineData("firstName", "")]
    [InlineData("lastName", "")]
    [InlineData("email", "")]
    [InlineData("email", "invalid-email")]
    [InlineData("username", null)]
    public async Task ProfileUpdateRejectsInvalidFields(string field, string? value)
    {
        using var client = MiPosApiFactory.CreateAuthenticatedClient(factory);
        var profile = new Dictionary<string, string>
        {
            ["username"] = "admin",
            ["firstName"] = "Ana",
            ["lastName"] = "Gómez",
            ["email"] = "admin@example.com"
        };
        profile[field] = value ?? new string('a', 101);

        using var response = await client.PutAsJsonAsync(ProfilePath, profile);
        await ErrorHandlingTests.AssertApiError(response, 400);
    }

    [Fact]
    public async Task UsernameEndpointIsRemoved()
    {
        using var client = MiPosApiFactory.CreateAuthenticatedClient(factory);
        using var response = await client.PutAsJsonAsync(ProfilePath + "/username", new { username = "admin" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [PostgresFact]
    public async Task LoginUsesGlobalUsernameAndIssuesATokenForTheUsersBusiness()
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

        foreach (var (username, password) in new[] { (first.Username, NewPassword()), ("missing", firstPassword), (first.Username, otherPassword) })
        {
            using var failure = await client.PostAsJsonAsync(LoginPath, new { username, password });
            var error = await ErrorHandlingTests.AssertApiError(failure, 401);
            Assert.Equal("Credenciales inválidas", error.GetProperty("title").GetString());
        }

        using var response = await client.PostAsJsonAsync(LoginPath, new { username = first.Username.ToUpperInvariant(), password = firstPassword });
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

        using var otherLogin = await client.PostAsJsonAsync(LoginPath, new { username = other.Username, password = otherPassword });
        Assert.Equal(HttpStatusCode.OK, otherLogin.StatusCode);
        var otherEnvelope = await otherLogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.Equal(other.BusinessId.ToString(), new JsonWebToken(otherEnvelope!.Data!.AccessToken).GetClaim("bid").Value);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var ownBusiness = await client.GetAsync($"/api/v1/businesses/{first.BusinessId}");
        Assert.Equal(HttpStatusCode.OK, ownBusiness.StatusCode);
        using var foreignBusiness = await client.GetAsync($"/api/v1/businesses/{other.BusinessId}");
        await ErrorHandlingTests.AssertApiError(foreignBusiness, 404);
        using var accepted = await client.GetAsync("/api/v1/typologies");
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        await AssertCurrentUserOperationsAsync(client, first, other, firstPassword);

        MiPosApiFactory.Authorize(api, client, first.BusinessId);
        using var createdBranch = await client.PostAsJsonAsync($"/api/v1/businesses/{first.BusinessId}/branches",
            new { code = "main", name = "Principal" });
        Assert.Equal(HttpStatusCode.Created, createdBranch.StatusCode);
        using var foreignWrite = await client.PostAsJsonAsync($"/api/v1/businesses/{other.BusinessId}/branches",
            new { code = "main", name = "Principal" });
        await ErrorHandlingTests.AssertApiError(foreignWrite, 404);
    }

    private static async Task AssertCurrentUserOperationsAsync(HttpClient client, User first, User other, string password)
    {
        using var original = await client.GetAsync(ProfilePath);
        Assert.Equal(HttpStatusCode.OK, original.StatusCode);
        var envelope = await original.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>();
        Assert.Null(envelope!.Error);
        Assert.Equal(new[] { "businessId", "email", "firstName", "id", "lastName", "roleCode", "username" },
            envelope.Data.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
        Assert.Equal(first.Id, envelope.Data.GetProperty("id").GetGuid());
        Assert.Equal(first.BusinessId, envelope.Data.GetProperty("businessId").GetGuid());
        Assert.Equal(RoleCodes.Administrator, envelope.Data.GetProperty("roleCode").GetString());

        using var duplicate = await client.PutAsJsonAsync(ProfilePath, new
        {
            username = $" {other.Username.ToUpperInvariant()} ",
            firstName = "María", lastName = "Suárez", email = "maria@example.com"
        });
        await ErrorHandlingTests.AssertApiError(duplicate, 409);
        using var unchanged = await client.GetAsync(ProfilePath);
        Assert.Equal(HttpStatusCode.OK, unchanged.StatusCode);
        var unchangedProfile = (await unchanged.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>())!.Data;
        Assert.Equal(first.Username, unchangedProfile.GetProperty("username").GetString());
        Assert.Equal(first.FirstName, unchangedProfile.GetProperty("firstName").GetString());
        Assert.Equal(first.LastName, unchangedProfile.GetProperty("lastName").GetString());
        Assert.Equal(first.Email, unchangedProfile.GetProperty("email").GetString());

        const string newUsername = "María-admin";
        using var profileUpdate = await client.PutAsJsonAsync(ProfilePath, new
        {
            username = $" {newUsername} ", firstName = "María", lastName = "Suárez", email = "maria@example.com",
            userId = other.Id, businessId = other.BusinessId, roleId = other.RoleId, permissions = Array.Empty<string>()
        });
        await AssertUpdatedAsync(profileUpdate);

        using var invalidPassword = await client.PutAsJsonAsync(ProfilePath + "/password",
            new { currentPassword = NewPassword(), newPassword = NewPassword() });
        await ErrorHandlingTests.AssertApiError(invalidPassword, 422);

        var newPassword = NewPassword();
        using var passwordUpdate = await client.PutAsJsonAsync(ProfilePath + "/password",
            new { currentPassword = password, newPassword });
        await AssertUpdatedAsync(passwordUpdate);

        using var updated = await client.GetAsync(ProfilePath);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedProfile = (await updated.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>())!.Data;
        Assert.Equal(first.Id, updatedProfile.GetProperty("id").GetGuid());
        Assert.Equal(first.BusinessId, updatedProfile.GetProperty("businessId").GetGuid());
        Assert.Equal(RoleCodes.Administrator, updatedProfile.GetProperty("roleCode").GetString());
        Assert.Equal("María", updatedProfile.GetProperty("firstName").GetString());
        Assert.Equal("Suárez", updatedProfile.GetProperty("lastName").GetString());
        Assert.Equal("maria@example.com", updatedProfile.GetProperty("email").GetString());
        Assert.Equal(newUsername, updatedProfile.GetProperty("username").GetString());

        using var oldPassword = await client.PostAsJsonAsync(LoginPath, new { username = newUsername, password });
        await ErrorHandlingTests.AssertApiError(oldPassword, 401);
        using var login = await client.PostAsJsonAsync(LoginPath, new { username = newUsername.ToUpperInvariant(), password = newPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    private static async Task AssertUpdatedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        Assert.True(envelope!.Data);
        Assert.Null(envelope.Error);
    }

    private static async Task<User> AddAdministratorAsync(MiPosDbContext dbContext, string code, string password)
    {
        var business = await new BusinessService(dbContext, new PasswordHasher<User>()).CreateBusinessAsync(new CreateBusinessRequest
        {
            Code = code, Name = code, CountryCode = "CO", CurrencyCode = "COP", TimeZone = "America/Bogota",
            Administrator = new InitialAdministratorRequest { Username = $"admin-{code}", FirstName = "Ana", LastName = "Gómez", Email = $"admin-{code}@example.com", Password = password }
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
