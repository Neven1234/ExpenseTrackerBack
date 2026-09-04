using ExpenseTracker.Application.DTOs.Expenses;

namespace ExpenseTracker.Application.Interfaces;

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseResponse>> ListAsync(int? year, int? month, Guid? categoryId, CancellationToken cancellationToken = default);
    Task<ExpenseResponse> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExpenseResponse> CreateAsync(ExpenseRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseResponse> UpdateAsync(Guid id, ExpenseRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
