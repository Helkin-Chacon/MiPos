using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MiPos.Api.Responses;
using MiPos.Application.Common.Exceptions;

namespace MiPos.IntegrationTests;

public sealed class ErrorHandlingTests(MiPosApiFactory factory)
    : IClassFixture<MiPosApiFactory>
{
    [Theory]
    [InlineData("missing", 404, "Registro no encontrado", "No existe un registro con ese identificador.")]
    [InlineData("conflict", 409, "Registro cerrado", "El registro ya está cerrado.")]
    [InlineData("validation", 422, "Monto inválido", "El monto debe ser mayor que cero.")]
    public async Task ApplicationExceptionReturnsApiError(
        string scenario, int status, string title, string message)
    {
        using var client = CreateClient();
        var path = $"/api/v1/test-errors/{scenario}";
        using var response = await client.GetAsync($"{path}?ignored=private");

        var error = await AssertApiError(response, status);
        Assert.Equal(title, error.GetProperty("title").GetString());
        Assert.Equal(message, error.GetProperty("message").GetString());
    }

    [Theory]
    [InlineData("unexpected")]
    [InlineData("internal-cancellation")]
    public async Task UnexpectedExceptionReturnsSafeApiError(string scenario)
    {
        using var client = CreateClient();
        var path = $"/api/v1/test-errors/{scenario}";
        using var response = await client.GetAsync(path);

        var error = await AssertApiError(response, 500);
        Assert.Equal("Error interno del servidor", error.GetProperty("title").GetString());
        Assert.Equal("Ocurrió un error inesperado al procesar la solicitud.", error.GetProperty("message").GetString());
        Assert.DoesNotContain("Password=private", error.GetRawText());
        Assert.DoesNotContain("SELECT", error.GetRawText());
        Assert.DoesNotContain("Exception", error.GetRawText());
    }

    [Fact]
    public async Task ValidRequestReturnsExplicitSuccessEnvelope()
    {
        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/test-errors", new { name = "MiPos" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "data", "error" }, body.EnumerateObject().Select(property => property.Name).Order());
        Assert.Equal("MiPos", body.GetProperty("data").GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("error").ValueKind);
    }

    [Fact]
    public async Task UnsupportedAcceptHeaderStillReturnsJsonApiError()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd("application/xml");
        const string path = "/api/v1/test-errors/missing";
        using var response = await client.GetAsync(path);

        var error = await AssertApiError(response, 404);
        Assert.Equal("Registro no encontrado", error.GetProperty("title").GetString());
        Assert.Equal("No existe un registro con ese identificador.", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task InvalidRequestReturnsValidationApiError()
    {
        using var client = CreateClient();
        const string path = "/api/v1/test-errors";
        using var response = await client.PostAsJsonAsync(path, new { });

        var error = await AssertApiError(response, 400);
        Assert.Equal("Solicitud inválida", error.GetProperty("title").GetString());
        Assert.Equal("Revisa los datos enviados e inténtalo de nuevo.", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task AbortedRequestUsesNativeCancellationHandling()
    {
        using var client = CreateClient();
        using var response = await client.GetAsync("/api/v1/test-errors/request-cancellation");

        Assert.Equal(499, (int)response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    private HttpClient CreateClient() =>
        MiPosApiFactory.CreateAuthenticatedClient(factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ErrorProbeController).Assembly))));

    internal static async Task<JsonElement> AssertApiError(HttpResponseMessage response, int status)
    {
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "data", "error" }, body.EnumerateObject().Select(property => property.Name).Order());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("data").ValueKind);
        var error = body.GetProperty("error");
        Assert.Equal(new[] { "message", "title", "traceId" },
            error.EnumerateObject().Select(property => property.Name).Order());
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("traceId").GetString()));
        return error;
    }
}

[ApiController]
[Route("api/v1/test-errors")]
public sealed class ErrorProbeController : ControllerBase
{
    private const string InternalMessage = "SELECT * FROM records; Password=private";

    [HttpGet("{scenario}")]
    public IActionResult Get(string scenario) => throw scenario switch
    {
        "missing" => new NotFoundException("Registro no encontrado", "No existe un registro con ese identificador."),
        "conflict" => new ConflictException("Registro cerrado", "El registro ya está cerrado."),
        "validation" => new BusinessValidationException("Monto inválido", "El monto debe ser mayor que cero."),
        "internal-cancellation" => new OperationCanceledException(InternalMessage),
        _ => new InvalidOperationException(InternalMessage)
    };

    [HttpGet("request-cancellation")]
    public IActionResult CancelRequest()
    {
        HttpContext.RequestAborted = new CancellationToken(true);
        throw new OperationCanceledException(HttpContext.RequestAborted);
    }

    [HttpPost]
    public ApiResponse<ProbeRequest> Post(ProbeRequest request) => new() { Data = request };

    public sealed record ProbeRequest([Required(ErrorMessage = "El nombre es obligatorio.")] string? Name);
}
