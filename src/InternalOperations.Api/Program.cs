using InternalOperations.Api.ErrorHandling;
using InternalOperations.Api.Extensions;
using InternalOperations.Application.Mappings;
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

if (app.Environment.IsDevelopment())
{
    // Generates the JSON spec file at /openapi/v1.json
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
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
