using InternalOperations.Domain.Users;

namespace InternalOperations.Domain.UnitTests;

public sealed class UserTests
{
    private static readonly DateTime CreatedAtUtc = new(2026, 8, 7, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateCanonicalizesProfileAndInitializesState()
    {
        var id = Guid.NewGuid();

        var user = User.Create(id, "  agent.one  ", "  Agent\t One ", null, CreatedAtUtc);

        Assert.Equal(id, user.Id);
        Assert.Equal("agent.one", user.UserName);
        Assert.Equal("Agent One", user.DisplayName);
        Assert.Null(user.DepartmentId);
        Assert.True(user.IsActive);
        Assert.False(user.IsDeleted);
        Assert.Equal(CreatedAtUtc, user.CreatedAtUtc);
        Assert.NotEqual(Guid.Empty, user.Version);
    }

    [Theory]
    [InlineData("", "Agent")]
    [InlineData("agent", "")]
    public void CreateRejectsMissingProfileValues(string userName, string displayName)
    {
        Assert.Throws<ArgumentException>(() => User.Create(Guid.NewGuid(), userName, displayName));
    }

    [Fact]
    public void CreateRejectsProfileValuesOverLimits()
    {
        Assert.Throws<ArgumentException>(() => User.Create(Guid.NewGuid(), new string('u', 257), "Agent"));
        Assert.Throws<ArgumentException>(() => User.Create(Guid.NewGuid(), "agent", new string('d', 201)));
    }

    [Fact]
    public void UpdateProfileAndAssignmentRotateVersion()
    {
        var user = User.Create(Guid.NewGuid(), "agent", "Agent");
        var initialVersion = user.Version;
        var departmentId = Guid.NewGuid();

        user.UpdateProfile("agent.updated", "Agent Updated", CreatedAtUtc);
        var profileVersion = user.Version;
        user.AssignDepartment(departmentId, CreatedAtUtc.AddMinutes(1));

        Assert.Equal("agent.updated", user.UserName);
        Assert.Equal("Agent Updated", user.DisplayName);
        Assert.NotEqual(initialVersion, profileVersion);
        Assert.Equal(departmentId, user.DepartmentId);
        Assert.NotEqual(profileVersion, user.Version);
    }

    [Fact]
    public void RepeatedAssignmentAndStatusRequestsAreIdempotent()
    {
        var departmentId = Guid.NewGuid();
        var user = User.Create(Guid.NewGuid(), "agent", "Agent", departmentId);
        var initialVersion = user.Version;

        user.AssignDepartment(departmentId, CreatedAtUtc);
        Assert.Equal(initialVersion, user.Version);

        user.Deactivate(CreatedAtUtc);
        var inactiveVersion = user.Version;
        user.Deactivate(CreatedAtUtc.AddMinutes(1));
        Assert.Equal(inactiveVersion, user.Version);

        user.RemoveDepartment(CreatedAtUtc.AddMinutes(2));
        Assert.Null(user.DepartmentId);
        user.Activate(CreatedAtUtc.AddMinutes(3));
        Assert.True(user.IsActive);
    }

    [Fact]
    public void AdministrativeChangeRotatesVersion()
    {
        var user = User.Create(Guid.NewGuid(), "agent", "Agent");
        var version = user.Version;

        user.RecordAdministrativeChange(CreatedAtUtc);

        Assert.NotEqual(version, user.Version);
        Assert.Equal(CreatedAtUtc, user.UpdatedAtUtc);
    }
}
