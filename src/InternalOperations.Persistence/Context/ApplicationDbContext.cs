using InternalOperations.Domain.Departments;
using InternalOperations.Domain.Tickets;
using InternalOperations.Domain.Users;
using InternalOperations.Persistence.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.Context;

public class ApplicationDbContext(DbContextOptions options) : IdentityDbContext<IdentityAccount, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<User> DomainUsers => Set<User>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<RefreshTokenSessionEntity> RefreshTokenSessions => Set<RefreshTokenSessionEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<IdentityAccount>().ToTable("IdentityUsers");
        builder.Entity<IdentityRole<Guid>>().ToTable("IdentityRoles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("IdentityUserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("IdentityUserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("IdentityUserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("IdentityRoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("IdentityUserTokens");
        builder.Entity<User>()
            .HasOne<IdentityAccount>()
            .WithOne()
            .HasForeignKey<User>(x => x.Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RefreshTokenSessionEntity>(entity =>
        {
            entity.ToTable("RefreshTokenSessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.DeviceDescription).HasMaxLength(200);
            entity.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
            entity.HasIndex(x => new { x.FamilyId, x.RevokedAtUtc });
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Ticket>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Entity<Ticket>().Property(e => e.Priority).HasConversion<string>().HasMaxLength(20);
    }
}
