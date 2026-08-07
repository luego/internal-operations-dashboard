using InternalOperations.Application.Abstractions.Authentication;
using InternalOperations.Domain.Users;
using InternalOperations.Persistence.Authentication;
using InternalOperations.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.IntegrationTests;

public sealed class IdentityModelTests
{
    [Fact]
    public void RefreshSessionHasUniqueHashRestrictedUserRelationshipAndConcurrencyToken()
    {
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var entity = context.Model.FindEntityType(typeof(RefreshTokenSessionEntity));
        Assert.NotNull(entity);
        Assert.Contains(entity!.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(RefreshTokenSessionEntity.TokenHash));
        Assert.True(entity.FindProperty(nameof(RefreshTokenSessionEntity.ConcurrencyStamp))!.IsConcurrencyToken);
        Assert.Equal(DeleteBehavior.Restrict, entity.GetForeignKeys().Single(x => x.PrincipalEntityType.ClrType == typeof(IdentityAccount)).DeleteBehavior);
        Assert.Equal(200, entity.FindProperty(nameof(RefreshTokenSessionEntity.DeviceDescription))!.GetMaxLength());
    }

    [Fact]
    public void RevokingTrackedRefreshSessionRotatesConcurrencyStamp()
    {
        var session = new RefreshTokenSessionEntity(new RefreshTokenSession(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('A', 64), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), null));
        var original = session.ConcurrencyStamp;
        session.Revoke(DateTimeOffset.UtcNow);
        Assert.NotEqual(original, session.ConcurrencyStamp);
    }

    [Fact]
    public void DomainUserUsesIdentityAccountAsRestrictedSharedPrimaryKeyPrincipal()
    {
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var entity = context.Model.FindEntityType(typeof(User));

        var relationship = Assert.Single(entity!.GetForeignKeys(), foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(IdentityAccount));
        Assert.True(relationship.IsUnique);
        Assert.Equal(DeleteBehavior.Restrict, relationship.DeleteBehavior);
        Assert.Equal(nameof(User.Id), Assert.Single(relationship.Properties).Name);
    }
}
