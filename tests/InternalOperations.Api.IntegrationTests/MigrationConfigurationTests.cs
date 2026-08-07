using InternalOperations.Api.Extensions;
using InternalOperations.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InternalOperations.Api.IntegrationTests;

public sealed class MigrationConfigurationTests
{
    [Theory]
    [InlineData("PostgreSql", "Host=localhost;Database=internal_operations;Username=test;Password=test", "Npgsql.EntityFrameworkCore.PostgreSQL", "InternalOperations.Persistence.Migrations.PostgreSql")]
    [InlineData("SqlServer", "Server=localhost;Database=internal_operations;User Id=test;Password=test;TrustServerCertificate=true", "Microsoft.EntityFrameworkCore.SqlServer", "InternalOperations.Persistence.Migrations.SqlServer")]
    public void ConfiguredProviderUsesItsDedicatedMigrationAssembly(
        string provider,
        string connectionString,
        string expectedProvider,
        string expectedMigrationsAssembly)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = provider,
                [$"ConnectionStrings:{provider}"] = connectionString,
            })
            .Build();
        var services = new ServiceCollection();

        services.AddPersistenceServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var relationalOptions = context.GetService<IDbContextOptions>()
            .Extensions
            .OfType<RelationalOptionsExtension>()
            .Single();

        Assert.Equal(expectedProvider, context.Database.ProviderName);
        Assert.Equal(expectedMigrationsAssembly, relationalOptions.MigrationsAssembly);
        var migrations = context.GetService<IMigrationsAssembly>().Migrations;
        Assert.Single(migrations);
        Assert.EndsWith("_InitialIdentityAndAccess", migrations.Keys.Single(), StringComparison.Ordinal);
    }
}
