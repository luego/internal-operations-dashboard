using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Persistence.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InternalOperations.Persistence;

public sealed class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private readonly ApplicationDbContext _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new PersistenceConcurrencyException();
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new PersistenceUniqueConstraintException();
        }
    }

    private static bool IsUniqueViolation(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return true;
            }

            if (current is SqlException { Number: 2601 or 2627 })
            {
                return true;
            }
        }

        return false;
    }
}
