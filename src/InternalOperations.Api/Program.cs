using InternalOperations.Api.Authentication;
using InternalOperations.Api.ErrorHandling;
using InternalOperations.Api.Extensions;
using InternalOperations.Application.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<TicketProfile>());
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddIdentityAccess(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT access token supplied as: Bearer {token}.",
        };
        if (document.Components.Schemas?.TryGetValue(nameof(InternalOperations.Api.Controllers.v1.CreateUserRequest), out var createUserSchema) == true
            && createUserSchema is OpenApiSchema requestSchema
            && requestSchema.Properties?.TryGetValue("initialPassword", out var passwordProperty) == true
            && passwordProperty is OpenApiSchema passwordSchema)
        {
            passwordSchema.Format = "password";
            passwordSchema.WriteOnly = true;
            passwordSchema.Example = null;
        }
        var bearerRequirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
        };
        foreach (var path in document.Paths.Values)
        {
            if (path.Operations is null) continue;
            foreach (var operation in path.Operations.Values)
            {
                if (operation.Security is null) operation.Security = [bearerRequirement];
            }
        }
        return Task.CompletedTask;
    });
    options.AddOperationTransformer((operation, context, _) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        // An empty requirement overrides the protected fallback for anonymous operations.
        if (metadata.OfType<IAllowAnonymous>().Any()) operation.Security = [];
        return Task.CompletedTask;
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();

    // Maps the beautiful Scalar UI at /scalar/v1
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Internal Operations API")
               .WithTheme(ScalarTheme.Kepler); // Choose from 10 built-in themes
    });
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Post
        && context.Request.Path.StartsWithSegments("/api/v1/auth")
        && !context.Request.HasJsonContentType())
    {
        context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
        await context.Response.WriteAsJsonAsync(new { type = "about:blank", title = "A JSON request body is required.", status = 415, code = "auth.unsupported_media_type" });
        return;
    }

    await next(context);
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/api/v1/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

await app.Services.GetRequiredService<DevelopmentIdentitySeeder>().SeedAsync();

app.Run();

public partial class Program;
