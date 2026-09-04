using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Application.Abstractions.Persistence;

public interface IMonthlyBudgetRepository
{
    Task<MonthlyBudget?> GetForUserAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyBudget>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyBudget>> ListUpToMonthAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default);
    void Add(MonthlyBudget budget);
    void Update(MonthlyBudget budget);
    void Remove(MonthlyBudget budget);
}
