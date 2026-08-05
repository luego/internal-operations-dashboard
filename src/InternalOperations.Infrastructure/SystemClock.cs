using System.Security.Claims;
using InternalOperations.Application;

namespace InternalOperations.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
