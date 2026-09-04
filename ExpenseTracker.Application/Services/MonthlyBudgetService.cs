using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Application.Abstractions.Security;
using ExpenseTracker.Application.DTOs.Budgets;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Application.Mapping;
using ExpenseTracker.Domain.Budgeting;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Exceptions;

namespace ExpenseTracker.Application.Services;

public class MonthlyBudgetService : IMonthlyBudgetService
{
    private readonly IMonthlyBudgetRepository _budgets;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public MonthlyBudgetService(IMonthlyBudgetRepository budgets, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _budgets = budgets;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<MonthlyBudgetResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var budgets = await _budgets.ListForUserAsync(_currentUser.Id, cancellationToken);
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
        var existing = await _budgets.GetForUserAsync(_currentUser.Id, request.Year, request.Month, cancellationToken);

        if (existing is not null)
            throw new ConflictException($"A budget for {request.Year}-{request.Month:00} already exists.");

        var budget = new MonthlyBudget
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.Id,
            Year = request.Year,
            Month = request.Month,
            Allowance = request.Allowance
        };

        _budgets.Add(budget);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var balance = await GetBalanceOrThrowAsync(budget.Year, budget.Month, cancellationToken);
        return balance.ToResponse();
    }

    public async Task<MonthlyBudgetResponse> UpdateAsync(int year, int month, UpdateMonthlyBudgetRequest request, CancellationToken cancellationToken = default)
    {
        var budget = await FindOrThrowAsync(year, month, cancellationToken);

        budget.Allowance = request.Allowance;
        _budgets.Update(budget);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var balance = await GetBalanceOrThrowAsync(year, month, cancellationToken);
        return balance.ToResponse();
    }

    public async Task DeleteAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var budget = await FindOrThrowAsync(year, month, cancellationToken);

        if (budget.Expenses.Any())
            throw new ConflictException("Budget still has expenses logged against it.");

        _budgets.Remove(budget);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<MonthlyBudget> FindOrThrowAsync(int year, int month, CancellationToken cancellationToken)
    {
        return await _budgets.GetForUserAsync(_currentUser.Id, year, month, cancellationToken)
            ?? throw new NotFoundException($"Budget for {year}-{month:00}");
    }

    private async Task<BudgetBalance> GetBalanceOrThrowAsync(int year, int month, CancellationToken cancellationToken)
    {
        var budgets = await _budgets.ListUpToMonthAsync(_currentUser.Id, year, month, cancellationToken);

        return BudgetLedger.Accumulate(budgets)
                   .LastOrDefault(balance => balance.Budget.Year == year && balance.Budget.Month == month)
               ?? throw new NotFoundException($"Budget for {year}-{month:00}");
    }
}
