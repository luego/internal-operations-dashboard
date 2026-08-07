using InternalOperations.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Identity;

namespace InternalOperations.Persistence.Authentication;

public sealed class IdentityAccount : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class RefreshTokenSessionEntity : RefreshTokenSession
{
    private RefreshTokenSessionEntity() : base(default, default, default, string.Empty, default, default, null) { }
    public RefreshTokenSessionEntity(RefreshTokenSession session) : base(session.Id, session.UserId, session.FamilyId, session.TokenHash, session.CreatedAtUtc, session.ExpiresAtUtc, session.DeviceDescription) { }
    public IdentityAccount User { get; private set; } = null!;
    public Guid ConcurrencyStamp { get; private set; } = Guid.NewGuid();
    public override void Revoke(DateTimeOffset now, Guid? replacementId = null)
    {
        base.Revoke(now, replacementId);
        ConcurrencyStamp = Guid.NewGuid();
    }
}
