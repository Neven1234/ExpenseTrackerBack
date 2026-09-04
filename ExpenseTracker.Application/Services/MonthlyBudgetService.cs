using ExpenseTracker.Application.Abstractions.Security;
using ExpenseTracker.Application.DTOs.Budgets;
using ExpenseTracker.Application.Mapping;
using ExpenseTracker.Application.Persistence;
using ExpenseTracker.Domain.Budgeting;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services;

public class MonthlyBudgetService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public MonthlyBudgetService(AppDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<MonthlyBudgetResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var budgets = await WithExpenses(_dbContext.MonthlyBudgets.AsNoTracking())
            .Where(budget => budget.UserId == _currentUser.Id)
            .OrderBy(budget => budget.Year).ThenBy(budget => budget.Month)
            .ToListAsync(cancellationToken);

        return BudgetLedger.Accumulate(budgets).Select(balance => balance.ToResponse()).ToList();
    }

    public async Task<MonthlyBudgetSummaryResponse> GetAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var balance = await GetBalanceOrThrowAsync(year, month, cancellationToken);

        var spendingByCategory = balance.Budget.Expenses
            .GroupBy(expense => new { expense.CategoryId, Name = expense.Category.Name })
            .Select(group => new CategorySpendResponse(group.Key.CategoryId, group.Key.Name, group.Sum(expense => expense.Amount)))
            .OrderByDescending(category => category.Amount)
            .ToList();

        return new MonthlyBudgetSummaryResponse(balance.ToResponse(), spendingByCategory);
    }

    public async Task<MonthlyBudgetResponse> CreateAsync(CreateMonthlyBudgetRequest request, CancellationToken cancellationToken = default)
    {
        var alreadyExists = await _dbContext.MonthlyBudgets.AnyAsync(
            budget => budget.UserId == _currentUser.Id && budget.Year == request.Year && budget.Month == request.Month,
            cancellationToken);

        if (alreadyExists)
            throw new ConflictException($"A budget for {request.Year}-{request.Month:00} already exists.");

        var budget = new MonthlyBudget
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.Id,
            Year = request.Year,
            Month = request.Month,
            Allowance = request.Allowance
        };

        _dbContext.MonthlyBudgets.Add(budget);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var balance = await GetBalanceOrThrowAsync(budget.Year, budget.Month, cancellationToken);
        return balance.ToResponse();
    }

    public async Task<MonthlyBudgetResponse> UpdateAsync(int year, int month, UpdateMonthlyBudgetRequest request, CancellationToken cancellationToken = default)
    {
        var budget = await FindOrThrowAsync(year, month, cancellationToken);

        budget.Allowance = request.Allowance;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var balance = await GetBalanceOrThrowAsync(year, month, cancellationToken);
        return balance.ToResponse();
    }

    public async Task DeleteAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var budget = await FindOrThrowAsync(year, month, cancellationToken);

        if (budget.Expenses.Any())
            throw new ConflictException("Budget still has expenses logged against it.");

        _dbContext.MonthlyBudgets.Remove(budget);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<MonthlyBudget> FindOrThrowAsync(int year, int month, CancellationToken cancellationToken)
    {
        return await WithExpenses(_dbContext.MonthlyBudgets)
                   .FirstOrDefaultAsync(
                       budget => budget.UserId == _currentUser.Id && budget.Year == year && budget.Month == month,
                       cancellationToken)
               ?? throw new NotFoundException($"Budget for {year}-{month:00}");
    }

    private async Task<BudgetBalance> GetBalanceOrThrowAsync(int year, int month, CancellationToken cancellationToken)
    {
        var budgets = await WithExpenses(_dbContext.MonthlyBudgets.AsNoTracking())
            .Where(budget => budget.UserId == _currentUser.Id
                && (budget.Year < year || (budget.Year == year && budget.Month <= month)))
            .OrderBy(budget => budget.Year).ThenBy(budget => budget.Month)
            .ToListAsync(cancellationToken);

        return BudgetLedger.Accumulate(budgets)
                   .LastOrDefault(balance => balance.Budget.Year == year && balance.Budget.Month == month)
               ?? throw new NotFoundException($"Budget for {year}-{month:00}");
    }

    private static IQueryable<MonthlyBudget> WithExpenses(IQueryable<MonthlyBudget> budgets) =>
        budgets.Include(budget => budget.Expenses).ThenInclude(expense => expense.Category);
}
