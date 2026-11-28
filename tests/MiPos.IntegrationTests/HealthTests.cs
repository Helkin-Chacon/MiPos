using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MiPos.IntegrationTests;

public sealed class HealthTests(MiPosApiFactory factory)
    : IClassFixture<MiPosApiFactory>
{
    [Fact]
    public async Task GetHealthReturnsHealthy()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
