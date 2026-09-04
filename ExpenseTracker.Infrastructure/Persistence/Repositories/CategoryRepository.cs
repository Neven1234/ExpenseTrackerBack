using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _dbContext;

    public CategoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Category>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories.AsNoTracking()
            .Where(category => category.UserId == userId)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .FirstOrDefaultAsync(category => category.Id == id && category.UserId == userId, cancellationToken);
    }

    public async Task<bool> NameExistsAsync(Guid userId, string name, Guid? excludedId = null, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories.AnyAsync(
            category => category.UserId == userId
                && category.Name == name
                && (excludedId == null || category.Id != excludedId),
            cancellationToken);
    }

    public async Task<bool> HasExpensesAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Expenses.AnyAsync(expense => expense.CategoryId == categoryId, cancellationToken);
    }

    public void Add(Category category) => _dbContext.Categories.Add(category);

    public void Update(Category category) => _dbContext.Categories.Update(category);

    public void Remove(Category category) => _dbContext.Categories.Remove(category);
}
