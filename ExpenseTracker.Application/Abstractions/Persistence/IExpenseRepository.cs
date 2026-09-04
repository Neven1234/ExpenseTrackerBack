using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Application.Abstractions.Persistence;

public interface IExpenseRepository
{
    Task<Expense?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Expense>> ListForUserAsync(Guid userId, int? year, int? month, Guid? categoryId, CancellationToken cancellationToken = default);
    void Add(Expense expense);
    void Update(Expense expense);
    void Remove(Expense expense);
}
