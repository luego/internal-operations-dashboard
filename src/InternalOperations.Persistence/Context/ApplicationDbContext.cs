using InternalOperations.Domain.Departments;
using InternalOperations.Domain.Tickets;
using InternalOperations.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.Context;

public class ApplicationDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<Department> Departments => Set<Department>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ticket>()
            .Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<Ticket>()
            .Property(e => e.Priority)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
