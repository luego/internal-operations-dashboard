using InternalOperations.Application;
using InternalOperations.Application.Features.Tickets;
using InternalOperations.Domain.Departments;
using InternalOperations.Domain.Tickets;
using InternalOperations.Persistence.Context;
using InternalOperations.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace InternalOperations.Persistence.IntegrationTests;

public sealed class TicketAdministrationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 7, 23, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAndGetUseActiveDepartmentAndReturnProjection()
    {
        await using var context = CreateContext();
        var department = Department.Create("Operations", null, Now.UtcDateTime);
        context.Departments.Add(department);
        await context.SaveChangesAsync();
        var service = new TicketAdministrationService(context, new FixedClock());

        var created = await service.CreateAsync(
            new CreateTicketCommand(" Printer  outage ", " Cannot print ", TicketPriority.High, department.Id, null),
            default);

        Assert.True(created.IsSuccess);
        Assert.True(created.Value!.Number > 0);
        Assert.Equal("Printer outage", created.Value.Title);
        Assert.Equal("Operations", created.Value.Department.Name);
        Assert.Equal(Now.UtcDateTime, created.Value.CreatedAtUtc);

        context.ChangeTracker.Clear();
        var loaded = await service.GetAsync(created.Value.Id, default);
        Assert.NotNull(loaded);
        Assert.Equal(created.Value.Id, loaded.Id);
    }

    [Fact]
    public async Task CreateRejectsInactiveDepartmentWithoutWritingTicket()
    {
        await using var context = CreateContext();
        var department = Department.Create("Archived", null);
        department.Deactivate(Now.UtcDateTime);
        context.Departments.Add(department);
        await context.SaveChangesAsync();
        var service = new TicketAdministrationService(context, new FixedClock());

        var result = await service.CreateAsync(
            new CreateTicketCommand("Printer outage", "Cannot print", TicketPriority.Medium, department.Id, null),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal("departments.inactive", result.Error!.Code);
        Assert.Empty(context.Tickets);
    }

    [Fact]
    public async Task ListUpdateAndStatusEnforceFiltersVersionAndTransitions()
    {
        await using var context = CreateContext();
        var department = Department.Create("Operations", null, Now.UtcDateTime);
        context.Departments.Add(department);
        await context.SaveChangesAsync();
        var service = new TicketAdministrationService(context, new FixedClock());
        var created = await service.CreateAsync(
            new CreateTicketCommand("Printer outage", "Cannot print", TicketPriority.High, department.Id, null),
            default);

        var page = await service.ListAsync(
            new TicketListFilter(1, 10, "printer", TicketStatus.Open, TicketPriority.High, department.Id, null),
            default);
        Assert.Single(page.Items);
        Assert.Equal(1, page.TotalCount);

        var updated = await service.UpdateAsync(
            new UpdateTicketCommand(
                created.Value!.Id,
                "Printer restored",
                "Printing works after restart",
                TicketPriority.Low,
                department.Id,
                null,
                created.Value.Version),
            default);
        Assert.True(updated.IsSuccess);
        Assert.Equal("Printer restored", updated.Value!.Title);

        var inProgress = await service.ChangeStatusAsync(
            new ChangeTicketStatusCommand(updated.Value.Id, TicketStatus.InProgress, updated.Value.Version),
            default);
        Assert.True(inProgress.IsSuccess);

        var invalid = await service.ChangeStatusAsync(
            new ChangeTicketStatusCommand(inProgress.Value!.Id, TicketStatus.Open, inProgress.Value.Version),
            default);
        Assert.False(invalid.IsSuccess);
        Assert.Equal("tickets.invalid_transition", invalid.Error!.Code);
    }

    [Fact]
    public async Task UpdateRejectsStaleVersionWithoutChangingTicket()
    {
        await using var context = CreateContext();
        var department = Department.Create("Operations", null, Now.UtcDateTime);
        context.Departments.Add(department);
        await context.SaveChangesAsync();
        var service = new TicketAdministrationService(context, new FixedClock());
        var created = await service.CreateAsync(
            new CreateTicketCommand("Printer outage", "Cannot print", TicketPriority.High, department.Id, null),
            default);

        var result = await service.UpdateAsync(
            new UpdateTicketCommand(
                created.Value!.Id,
                "Changed",
                "Changed description",
                TicketPriority.Low,
                department.Id,
                null,
                Guid.NewGuid()),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal("tickets.version_conflict", result.Error!.Code);
    }

    [Fact]
    public void TicketMappingGeneratesNumberOnAddAndUsesVersionForConcurrency()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Ticket))!;

        Assert.Equal(ValueGenerated.OnAdd, entity.FindProperty(nameof(Ticket.Number))!.ValueGenerated);
        Assert.True(entity.FindProperty(nameof(Ticket.Version))!.IsConcurrencyToken);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
