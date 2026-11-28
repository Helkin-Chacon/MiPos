using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiPos.Api.Identity;
using MiPos.Application.Identity.Models;
using MiPos.Domain.Constants.Identity;

namespace MiPos.IntegrationTests;

public class MiPosApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = key,
            ["Installation:BusinessCode"] = "market"
        }));
    }

    internal static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        Authorize(factory, client, Guid.NewGuid());
        return client;
    }

    internal static void Authorize(WebApplicationFactory<Program> factory, HttpClient client, Guid businessId)
    {
        var generator = factory.Services.GetRequiredService<JwtTokenGenerator>();
        var token = generator.Generate(new AuthenticatedUser
        {
            UserId = Guid.NewGuid(), BusinessId = businessId, RoleCode = RoleCodes.Cashier,
            Permissions = []
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
