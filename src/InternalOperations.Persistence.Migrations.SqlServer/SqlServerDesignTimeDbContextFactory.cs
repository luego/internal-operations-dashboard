using InternalOperations.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InternalOperations.Persistence.Migrations.SqlServer;

public sealed class SqlServerDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=internal_operations_design;Trusted_Connection=true",
                provider => provider.MigrationsAssembly(MigrationAssemblyNames.SqlServer))
            .Options;

        return new ApplicationDbContext(options);
    }
}
