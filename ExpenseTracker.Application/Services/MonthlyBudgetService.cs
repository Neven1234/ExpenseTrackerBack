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
    private const string ExpenseCategoryInclude = "Expenses.Category";

    private readonly IRepository<MonthlyBudget> _budgets;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public MonthlyBudgetService(IRepository<MonthlyBudget> budgets, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _budgets = budgets;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<MonthlyBudgetResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var budgets = await _budgets.GetAllAsync(
            budget => budget.UserId == _currentUser.Id,
            OrderByMonth,
            ExpenseCategoryInclude);

        return BudgetLedger.Accumulate(budgets).Select(balance => balance.ToResponse()).ToList();
    }

    public async Task<MonthlyBudgetSummaryResponse> GetAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var balance = await GetBalanceOrThrowAsync(year, month);

        var spendingByCategory = balance.Budget.Expenses
            .GroupBy(expense => new { expense.CategoryId, Name = expense.Category.Name })
            .Select(group => new CategorySpendResponse(group.Key.CategoryId, group.Key.Name, group.Sum(expense => expense.Amount)))
            .OrderByDescending(category => category.Amount)
            .ToList();

        return new MonthlyBudgetSummaryResponse(balance.ToResponse(), spendingByCategory);
    }

    public async Task<MonthlyBudgetResponse> CreateAsync(CreateMonthlyBudgetRequest request, CancellationToken cancellationToken = default)
    {
        var alreadyExists = await _budgets.AnyAsync(
            budget => budget.UserId == _currentUser.Id && budget.Year == request.Year && budget.Month == request.Month);

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

        _budgets.Add(budget);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var balance = await GetBalanceOrThrowAsync(budget.Year, budget.Month);
        return balance.ToResponse();
    }

    public async Task<MonthlyBudgetResponse> UpdateAsync(int year, int month, UpdateMonthlyBudgetRequest request, CancellationToken cancellationToken = default)
    {
        var budget = await FindOrThrowAsync(year, month);

        budget.Allowance = request.Allowance;
        _budgets.Update(budget);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var balance = await GetBalanceOrThrowAsync(year, month);
        return balance.ToResponse();
    }

    public async Task DeleteAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var budget = await FindOrThrowAsync(year, month);

        if (budget.Expenses.Any())
            throw new ConflictException("Budget still has expenses logged against it.");

        _budgets.Remove(budget);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<MonthlyBudget> FindOrThrowAsync(int year, int month)
    {
        return await _budgets.GetAsync(
                   budget => budget.UserId == _currentUser.Id && budget.Year == year && budget.Month == month,
                   ExpenseCategoryInclude)
               ?? throw new NotFoundException($"Budget for {year}-{month:00}");
    }

    private async Task<BudgetBalance> GetBalanceOrThrowAsync(int year, int month)
    {
        var budgets = await _budgets.GetAllAsync(
            budget => budget.UserId == _currentUser.Id
                && (budget.Year < year || (budget.Year == year && budget.Month <= month)),
            OrderByMonth,
            ExpenseCategoryInclude);

        return BudgetLedger.Accumulate(budgets)
                   .LastOrDefault(balance => balance.Budget.Year == year && balance.Budget.Month == month)
               ?? throw new NotFoundException($"Budget for {year}-{month:00}");
    }

    private static IOrderedQueryable<MonthlyBudget> OrderByMonth(IQueryable<MonthlyBudget> budgets) =>
        budgets.OrderBy(budget => budget.Year).ThenBy(budget => budget.Month);
}
