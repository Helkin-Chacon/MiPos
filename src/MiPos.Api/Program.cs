using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using MiPos.Api.Errors;
using MiPos.Api.Identity;
using MiPos.Api.Responses;
using MiPos.Application.Common.Interfaces;
using MiPos.Application.Identity.Interfaces;
using MiPos.Application.Identity.Services;
using MiPos.Application.Organization.Interfaces;
using MiPos.Application.Organization.Services;
using MiPos.Application.Typologies.Interfaces;
using MiPos.Application.Typologies.Services;
using MiPos.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressMapClientErrors = true;
        options.InvalidModelStateResponseFactory = context =>
        {
            var error = ApiError.FromRequest(context.HttpContext, "Solicitud inválida",
                "Revisa los datos enviados e inténtalo de nuevo.");
            return new JsonResult(new ApiResponse<object> { Error = error })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        };
    });
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };
        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, _) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any())
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, context.Document)] = []
            });
        }

        return Task.CompletedTask;
    });
});
builder.Services.AddMiPosAuthentication(builder.Configuration);
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ITypologyService, TypologyService>();
builder.Services.AddScoped<IBusinessService, BusinessService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddDbContext<IMiPosDbContext, MiPosDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required to use PostgreSQL.");
    }

    options.UseNpgsql(connectionString);
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
    var httpContext = context.HttpContext;
    var status = httpContext.Response.StatusCode;
    if (!httpContext.Request.Path.StartsWithSegments("/api") ||
        httpContext.RequestAborted.IsCancellationRequested ||
        status == StatusCodes.Status499ClientClosedRequest)
    {
        return;
    }

    var (title, message) = status switch
    {
        StatusCodes.Status400BadRequest => ("Solicitud inválida", "Revisa los datos enviados e inténtalo de nuevo."),
        StatusCodes.Status401Unauthorized => ("Autenticación requerida", "Inicia sesión para continuar."),
        StatusCodes.Status403Forbidden => ("Acceso denegado", "No tienes permiso para realizar esta operación."),
        StatusCodes.Status404NotFound => ("Recurso no encontrado", "No se encontró el recurso solicitado."),
        StatusCodes.Status405MethodNotAllowed => ("Método no permitido", "El recurso no admite el método solicitado."),
        StatusCodes.Status415UnsupportedMediaType => ("Formato no admitido", "El formato del contenido enviado no es compatible."),
        >= 500 => ("Error del servidor", "Ocurrió un error al procesar la solicitud."),
        _ => ("No se pudo procesar la solicitud", "Revisa la solicitud e inténtalo de nuevo.")
    };

    var error = ApiError.FromRequest(httpContext, title, message);
    await httpContext.Response.WriteAsJsonAsync(new ApiResponse<object> { Error = error }, httpContext.RequestAborted);
});

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.DisableDefaultFonts()
        .AddPreferredSecuritySchemes(JwtBearerDefaults.AuthenticationScheme));
}

app.MapControllers();

app.Run();

public partial class Program;
