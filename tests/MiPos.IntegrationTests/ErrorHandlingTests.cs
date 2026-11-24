using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MiPos.Application.Common.Exceptions;

namespace MiPos.IntegrationTests;

public sealed class ErrorHandlingTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("missing", 404, "Registro no encontrado", "No existe un registro con ese identificador.")]
    [InlineData("conflict", 409, "Registro cerrado", "El registro ya está cerrado.")]
    [InlineData("validation", 422, "Monto inválido", "El monto debe ser mayor que cero.")]
    public async Task ApplicationExceptionReturnsProblemDetails(
        string scenario, int status, string title, string detail)
    {
        using var client = CreateClient();
        var path = $"/test-errors/{scenario}";
        using var response = await client.GetAsync($"{path}?ignored=private");

        var problem = await AssertProblemDetails(response, status, path);
        Assert.Equal(title, problem.GetProperty("title").GetString());
        Assert.Equal(detail, problem.GetProperty("detail").GetString());
        Assert.False(problem.TryGetProperty("errors", out _));
    }

    [Theory]
    [InlineData("unexpected")]
    [InlineData("internal-cancellation")]
    public async Task UnexpectedExceptionReturnsSafeProblemDetails(string scenario)
    {
        using var client = CreateClient();
        var path = $"/test-errors/{scenario}";
        using var response = await client.GetAsync(path);

        var problem = await AssertProblemDetails(response, 500, path);
        Assert.Equal("Error interno del servidor", problem.GetProperty("title").GetString());
        Assert.Equal("Ocurrió un error inesperado al procesar la solicitud.", problem.GetProperty("detail").GetString());
        Assert.DoesNotContain("Password=private", problem.GetRawText());
        Assert.DoesNotContain("SELECT", problem.GetRawText());
        Assert.DoesNotContain("Exception", problem.GetRawText());
    }

    [Fact]
    public async Task UnsupportedAcceptHeaderFallsBackToProblemDetailsJson()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd("application/xml");
        const string path = "/test-errors/missing";
        using var response = await client.GetAsync(path);

        var problem = await AssertProblemDetails(response, 404, path);
        Assert.Equal("Registro no encontrado", problem.GetProperty("title").GetString());
        Assert.Equal("No existe un registro con ese identificador.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task InvalidRequestReturnsNativeValidationProblemDetails()
    {
        using var client = CreateClient();
        const string path = "/test-errors";
        using var response = await client.PostAsJsonAsync(path, new { });

        var problem = await AssertProblemDetails(response, 400, path);
        Assert.Equal("La solicitud contiene errores de validación.", problem.GetProperty("title").GetString());
        Assert.Equal("El nombre es obligatorio.", problem.GetProperty("errors").GetProperty("Name")[0].GetString());
    }

    [Fact]
    public async Task AbortedRequestUsesNativeCancellationHandling()
    {
        using var client = CreateClient();
        using var response = await client.GetAsync("/test-errors/request-cancellation");

        Assert.Equal(499, (int)response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    private HttpClient CreateClient() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ErrorProbeController).Assembly)))
        .CreateClient();

    private static async Task<JsonElement> AssertProblemDetails(
        HttpResponseMessage response, int status, string instance)
    {
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, problem.GetProperty("status").GetInt32());
        Assert.Equal(instance, problem.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
        Assert.False(problem.TryGetProperty("code", out _));
        return problem;
    }
}

[ApiController]
[Route("test-errors")]
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
    public IActionResult Post(ProbeRequest request) => Ok();

    public sealed record ProbeRequest([Required(ErrorMessage = "El nombre es obligatorio.")] string? Name);
}
