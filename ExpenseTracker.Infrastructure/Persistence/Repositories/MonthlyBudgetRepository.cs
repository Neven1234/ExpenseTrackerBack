using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

public class MonthlyBudgetRepository : IMonthlyBudgetRepository
{
    private readonly AppDbContext _dbContext;

    public MonthlyBudgetRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MonthlyBudget?> GetForUserAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default)
    {
        return await WithExpenses(_dbContext.MonthlyBudgets)
            .FirstOrDefaultAsync(
                budget => budget.UserId == userId && budget.Year == year && budget.Month == month,
                cancellationToken);
    }

    public async Task<IReadOnlyList<MonthlyBudget>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await WithExpenses(_dbContext.MonthlyBudgets.AsNoTracking())
            .Where(budget => budget.UserId == userId)
            .OrderBy(budget => budget.Year).ThenBy(budget => budget.Month)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MonthlyBudget>> ListUpToMonthAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default)
    {
        return await WithExpenses(_dbContext.MonthlyBudgets.AsNoTracking())
            .Where(budget => budget.UserId == userId
                && (budget.Year < year || (budget.Year == year && budget.Month <= month)))
            .OrderBy(budget => budget.Year).ThenBy(budget => budget.Month)
            .ToListAsync(cancellationToken);
    }

    public void Add(MonthlyBudget budget) => _dbContext.MonthlyBudgets.Add(budget);

    public void Update(MonthlyBudget budget) => _dbContext.MonthlyBudgets.Update(budget);

    public void Remove(MonthlyBudget budget) => _dbContext.MonthlyBudgets.Remove(budget);

    private static IQueryable<MonthlyBudget> WithExpenses(IQueryable<MonthlyBudget> budgets) =>
        budgets.Include(budget => budget.Expenses).ThenInclude(expense => expense.Category);
}
