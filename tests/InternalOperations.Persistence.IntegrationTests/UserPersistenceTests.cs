using InternalOperations.Domain.Users;
using InternalOperations.Persistence.Authentication;
using InternalOperations.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace InternalOperations.Persistence.IntegrationTests;

public sealed class UserPersistenceTests
{
    [Fact]
    public void ModelDefinesUserLimitsIndexesConcurrencyAndSharedIdentityKey()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        var user = context.Model.FindEntityType(typeof(User))!;

        Assert.Equal(256, user.FindProperty(nameof(User.UserName))!.GetMaxLength());
        Assert.Equal(200, user.FindProperty(nameof(User.DisplayName))!.GetMaxLength());
        Assert.True(user.FindProperty(nameof(User.Version))!.IsConcurrencyToken);
        Assert.Contains(user.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(User.DepartmentId), nameof(User.IsActive)]));
        Assert.Contains(user.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(IdentityAccount)
            && foreignKey.Properties.Single().Name == nameof(User.Id)
            && foreignKey.DeleteBehavior == DeleteBehavior.Restrict);

        var identity = context.Model.FindEntityType(typeof(IdentityAccount))!;
        Assert.Contains(identity.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(IdentityAccount.NormalizedEmail)]));
    }
}
