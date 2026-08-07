using InternalOperations.Domain.Departments;

namespace InternalOperations.Domain.UnitTests;

public sealed class DepartmentTests
{
    [Fact]
    public void CreateCanonicalizesValuesAndInitializesState()
    {
        var department = Department.Create("  Customer\t  Support  ", null);

        Assert.Equal("Customer Support", department.Name);
        Assert.Equal("CUSTOMER SUPPORT", department.NormalizedName);
        Assert.Equal(string.Empty, department.Description);
        Assert.True(department.IsActive);
        Assert.False(department.IsDeleted);
        Assert.NotEqual(Guid.Empty, department.Version);
    }

    [Fact]
    public void CreateUsesUnicodeCompatibilityNormalizationBeforeInvariantCase()
    {
        var department = Department.Create(" Ｏｐｓ ", " Internal operations ");

        Assert.Equal("Ops", department.Name);
        Assert.Equal("OPS", department.NormalizedName);
        Assert.Equal("Internal operations", department.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRejectsEmptyName(string name)
    {
        Assert.Throws<ArgumentException>(() => Department.Create(name, null));
    }

    [Fact]
    public void CreateRejectsValuesOverMaximumLength()
    {
        Assert.Throws<ArgumentException>(() => Department.Create(new string('N', 101), null));
        Assert.Throws<ArgumentException>(() => Department.Create("Operations", new string('D', 501)));
    }

    [Fact]
    public void UpdateCanonicalizesValuesAndRotatesVersion()
    {
        var department = Department.Create("Operations", null);
        var originalVersion = department.Version;
        var updatedAtUtc = new DateTime(2026, 8, 7, 20, 0, 0, DateTimeKind.Utc);

        department.Update("  Customer\tCare ", " Priority  requests ", updatedAtUtc);

        Assert.Equal("Customer Care", department.Name);
        Assert.Equal("CUSTOMER CARE", department.NormalizedName);
        Assert.Equal("Priority requests", department.Description);
        Assert.Equal(updatedAtUtc, department.UpdatedAtUtc);
        Assert.NotEqual(originalVersion, department.Version);
    }

    [Fact]
    public void UpdateWithSameCanonicalValuesIsIdempotent()
    {
        var department = Department.Create("Customer Care", "Priority requests");
        var originalVersion = department.Version;

        department.Update(" Customer  Care ", " Priority requests ", DateTime.UtcNow);

        Assert.Equal(originalVersion, department.Version);
        Assert.Null(department.UpdatedAtUtc);
    }

    [Fact]
    public void StatusChangesRotateVersionAndRepeatedRequestIsIdempotent()
    {
        var department = Department.Create("Operations", null);
        var originalVersion = department.Version;
        var updatedAtUtc = new DateTime(2026, 8, 7, 20, 0, 0, DateTimeKind.Utc);

        department.Deactivate(updatedAtUtc);
        var inactiveVersion = department.Version;
        department.Deactivate(updatedAtUtc.AddMinutes(1));

        Assert.False(department.IsActive);
        Assert.NotEqual(originalVersion, inactiveVersion);
        Assert.Equal(inactiveVersion, department.Version);

        department.Activate(updatedAtUtc.AddMinutes(2));
        Assert.True(department.IsActive);
        Assert.NotEqual(inactiveVersion, department.Version);
    }
}
