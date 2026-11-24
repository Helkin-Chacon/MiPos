using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiPos.Api.Errors;
using MiPos.Application.Common.Interfaces;
using MiPos.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance ??= $"{context.HttpContext.Request.PathBase}{context.HttpContext.Request.Path}";
        context.ProblemDetails.Extensions.TryAdd("traceId",
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
        if (context.ProblemDetails is ValidationProblemDetails { Status: StatusCodes.Status400BadRequest })
        {
            context.ProblemDetails.Title = "La solicitud contiene errores de validación.";
        }
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.DisableDefaultFonts());
}

app.MapControllers();

app.Run();

public partial class Program;
