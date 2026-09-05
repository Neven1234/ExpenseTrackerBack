using ExpenseTracker.Application.DTOs.Budgets;

namespace ExpenseTracker.Application.Interfaces;

public interface IMonthlyBudgetService
{
    Task<IReadOnlyList<MonthlyBudgetResponse>> ListAsync(CancellationToken cancellationToken = default);
    /// <summary>Returns null when the user has no budget for that month.</summary>
    Task<MonthlyBudgetSummaryResponse?> GetAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<MonthlyBudgetResponse> CreateAsync(CreateMonthlyBudgetRequest request, CancellationToken cancellationToken = default);
    Task<MonthlyBudgetResponse> UpdateAsync(int year, int month, UpdateMonthlyBudgetRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int year, int month, CancellationToken cancellationToken = default);
}
