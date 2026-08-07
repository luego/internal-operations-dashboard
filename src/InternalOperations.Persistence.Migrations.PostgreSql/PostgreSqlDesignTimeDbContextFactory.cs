using InternalOperations.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InternalOperations.Persistence.Migrations.PostgreSql;

public sealed class PostgreSqlDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=internal_operations_design",
                provider => provider.MigrationsAssembly(MigrationAssemblyNames.PostgreSql))
            .Options;

        return new ApplicationDbContext(options);
    }
}
