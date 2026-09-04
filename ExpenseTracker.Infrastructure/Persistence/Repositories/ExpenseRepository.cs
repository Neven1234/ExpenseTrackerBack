using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _dbContext;

    public ExpenseRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Expense?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Expenses
            .Include(expense => expense.Category)
            .FirstOrDefaultAsync(
                expense => expense.Id == id && expense.MonthlyBudget.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Expense>> ListForUserAsync(
        Guid userId,
        int? year,
        int? month,
        Guid? categoryId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Expenses.AsNoTracking()
            .Include(expense => expense.Category)
            .Where(expense => expense.MonthlyBudget.UserId == userId);

        if (year.HasValue)
            query = query.Where(expense => expense.MonthlyBudget.Year == year.Value);

        if (month.HasValue)
            query = query.Where(expense => expense.MonthlyBudget.Month == month.Value);

        if (categoryId.HasValue)
            query = query.Where(expense => expense.CategoryId == categoryId.Value);

        return await query
            .OrderByDescending(expense => expense.SpentOn)
            .ToListAsync(cancellationToken);
    }

    public void Add(Expense expense) => _dbContext.Expenses.Add(expense);

    public void Update(Expense expense) => _dbContext.Expenses.Update(expense);

    public void Remove(Expense expense) => _dbContext.Expenses.Remove(expense);
}
