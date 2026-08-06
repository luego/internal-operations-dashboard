using InternalOperations.Api.ErrorHandling;
using InternalOperations.Api.Extensions;
using InternalOperations.Application.Mappings;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<TicketProfile>());
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
var app = builder.Build();

app.MapControllers();
app.MapGet("/api/v1/health", () => Results.Ok(new { status = "ok" }));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Maps the beautiful Scalar UI at /scalar/v1
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Internal Operations API")
               .WithTheme(ScalarTheme.Kepler); // Choose from 10 built-in themes
    });
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.Run();
