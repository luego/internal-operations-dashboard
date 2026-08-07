using InternalOperations.Application.Features.Departments;
using InternalOperations.Domain.Departments;
using InternalOperations.Persistence.Context;
using InternalOperations.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.IntegrationTests;

public sealed class DepartmentPersistenceTests
{
    [Fact]
    public async Task RepositoryAndReadServicePersistAndProjectDepartment()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var now = new DateTime(2026, 8, 7, 18, 0, 0, DateTimeKind.Utc);
        var department = Department.Create(" Customer  Support ", " Helps customers ", now);

        await using var context = new ApplicationDbContext(options);
        var repository = new DepartmentRepository(context);
        await repository.AddAsync(department, default);
        await context.SaveChangesAsync();

        Assert.True(await repository.NormalizedNameExistsAsync("CUSTOMER SUPPORT", null, default));
        var dto = await new DepartmentReadService(context).GetAsync(department.Id, default);
        Assert.NotNull(dto);
        Assert.Equal("Customer Support", dto.Name);
        Assert.Equal("Helps customers", dto.Description);
        Assert.Equal(now, dto.CreatedAtUtc);
    }

    [Fact]
    public void ModelDefinesUniqueNormalizedNameAndConcurrencyVersion()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Department))!;

        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Department.NormalizedName));
        Assert.True(entity.FindProperty(nameof(Department.Version))!.IsConcurrencyToken);
    }

    [Fact]
    public async Task ListFiltersSortsAndPaginatesInReadService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        var operations = Department.Create("Operations", "Internal work");
        var support = Department.Create("Customer Support", "Customer requests");
        support.Deactivate(DateTime.UtcNow);
        context.Departments.AddRange(operations, support, Department.Create("Sales", null));
        await context.SaveChangesAsync();

        var result = await new DepartmentReadService(context).ListAsync(
            new DepartmentListFilter(1, 10, "customer", false, "name", "asc"),
            default);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(support.Id, Assert.Single(result.Items).Id);
    }
}
