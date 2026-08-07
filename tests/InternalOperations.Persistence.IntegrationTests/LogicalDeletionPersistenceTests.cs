using InternalOperations.Domain.Departments;
using InternalOperations.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.IntegrationTests;

public sealed class LogicalDeletionPersistenceTests
{
    [Fact]
    public async Task RemovingBusinessEntityPersistsLogicalDeletionAndQueryFilterHidesIt()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var department = Department.Create("Operations", null);

        await using (var context = new ApplicationDbContext(options))
        {
            context.Departments.Add(department);
            await context.SaveChangesAsync();
            context.Remove(department);
            await context.SaveChangesAsync();
        }

        await using (var context = new ApplicationDbContext(options))
        {
            Assert.Empty(await context.Departments.ToArrayAsync());
            var persisted = await context.Departments.IgnoreQueryFilters().SingleAsync();
            Assert.True(persisted.IsDeleted);
        }
    }
}
