using InternalOperations.Domain.Common;

namespace InternalOperations.Domain.UnitTests;

public sealed class LogicalDeletionTests
{
    [Fact]
    public void EntityStartsVisibleAndDeleteAndRestoreAreIdempotent()
    {
        var entity = new TestEntity();

        Assert.False(entity.IsDeleted);

        entity.Delete();
        entity.Delete();
        Assert.True(entity.IsDeleted);

        entity.Restore();
        entity.Restore();
        Assert.False(entity.IsDeleted);
    }

    private sealed class TestEntity : BaseEntity;
}
