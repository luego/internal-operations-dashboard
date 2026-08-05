using InternalOperations.Domain.Common;
using InternalOperations.Persistence.Context;
using InternalOperations.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.IntegrationTests;

public sealed class UnitOfWorkTests
{
    [Fact]
    public async Task UnitOfWorkCommitsTrackedChanges()
    {
        var options = new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new TestApplicationDbContext(options);
        //var repository = new GenericRepository<TestEntity>(context);
        var unitOfWork = new UnitOfWork(context, new TicketRepository(context), new UserRepository(context));

        await unitOfWork.Tickets.AddAsync(new Domain.Tickets.Ticket { Title = "Alpha" });
        await unitOfWork.SaveChangesAsync();

        var stored = await context.TestEntities.SingleAsync();
        Assert.Equal("Alpha", stored.Title);
    }

    private sealed class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<TestApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TestEntity> TestEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestEntity>().HasKey(x => x.Id);
        }
    }

    private sealed class TestEntity : BaseEntity
    {

        public string Title { get; set; } = string.Empty;
    }
}
