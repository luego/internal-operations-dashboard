using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace InternalOperations.Persistence.Context;

internal sealed class TicketNumberValueGenerator : ValueGenerator<int>
{
    private static int _current;

    public override bool GeneratesTemporaryValues => false;

    public override int Next(EntityEntry entry) => Interlocked.Increment(ref _current);
}
