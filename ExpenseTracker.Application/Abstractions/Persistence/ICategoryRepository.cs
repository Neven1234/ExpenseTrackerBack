using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Application.Abstractions.Persistence;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Category?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(Guid userId, string name, Guid? excludedId = null, CancellationToken cancellationToken = default);
    Task<bool> HasExpensesAsync(Guid categoryId, CancellationToken cancellationToken = default);
    void Add(Category category);
    void Update(Category category);
    void Remove(Category category);
}
