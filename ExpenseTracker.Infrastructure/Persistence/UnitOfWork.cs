using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    // SQL Server: duplicate key in a unique index, and unique constraint violation.
    private const int DuplicateKeyError = 2601;
    private const int UniqueConstraintError = 2627;

    private readonly AppDbContext _dbContext;

    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // The services check for duplicates first, but two concurrent requests can both pass
            // that check and only lose the race here. Report it the same way the check would.
            throw new ConflictException("That record already exists.", exception);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: DuplicateKeyError or UniqueConstraintError };
}
