using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Application.Abstractions.Security;
using ExpenseTracker.Application.DTOs.Expenses;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Application.Mapping;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Exceptions;

namespace ExpenseTracker.Application.Services;

public class ExpenseService : IExpenseService
{
    private const string CategoryInclude = "Category";

    private readonly IRepository<Expense> _expenses;
    private readonly IRepository<Category> _categories;
    private readonly IRepository<MonthlyBudget> _budgets;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ExpenseService(
        IRepository<Expense> expenses,
        IRepository<Category> categories,
        IRepository<MonthlyBudget> budgets,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _expenses = expenses;
        _categories = categories;
        _budgets = budgets;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ExpenseResponse>> ListAsync(int? year, int? month, Guid? categoryId, CancellationToken cancellationToken = default)
    {
        var expenses = await _expenses.GetAllAsync(
            expense => expense.MonthlyBudget.UserId == _currentUser.Id
                && (year == null || expense.MonthlyBudget.Year == year)
                && (month == null || expense.MonthlyBudget.Month == month)
                && (categoryId == null || expense.CategoryId == categoryId),
            query => query.OrderByDescending(expense => expense.SpentOn),
            CategoryInclude);

        return expenses.Select(expense => expense.ToResponse()).ToList();
    }

    public async Task<ExpenseResponse> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await FindOrThrowAsync(id);
        return expense.ToResponse();
    }

    public async Task<ExpenseResponse> CreateAsync(ExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var category = await FindCategoryOrThrowAsync(request.CategoryId);
        var budget = await FindBudgetOrThrowAsync(request.SpentOn);

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            MonthlyBudgetId = budget.Id,
            CategoryId = category.Id,
            Amount = request.Amount,
            Note = request.Note.Trim(),
            SpentOn = request.SpentOn
        };

        _expenses.Add(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return expense.ToResponse(category.Name);
    }

    public async Task<ExpenseResponse> UpdateAsync(Guid id, ExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var expense = await FindOrThrowAsync(id);
        var category = await FindCategoryOrThrowAsync(request.CategoryId);
        var budget = await FindBudgetOrThrowAsync(request.SpentOn);

        expense.MonthlyBudgetId = budget.Id;
        expense.CategoryId = category.Id;
        expense.Amount = request.Amount;
        expense.Note = request.Note.Trim();
        expense.SpentOn = request.SpentOn;

        _expenses.Update(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return expense.ToResponse(category.Name);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await FindOrThrowAsync(id);

        _expenses.Remove(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Expense> FindOrThrowAsync(Guid id)
    {
        return await _expenses.GetAsync(
                   expense => expense.Id == id && expense.MonthlyBudget.UserId == _currentUser.Id,
                   CategoryInclude)
               ?? throw new NotFoundException("Expense");
    }

    private async Task<Category> FindCategoryOrThrowAsync(Guid categoryId)
    {
        return await _categories.GetAsync(category => category.Id == categoryId && category.UserId == _currentUser.Id)
            ?? throw new NotFoundException("Category");
    }

    private async Task<MonthlyBudget> FindBudgetOrThrowAsync(DateOnly spentOn)
    {
        return await _budgets.GetAsync(
                   budget => budget.UserId == _currentUser.Id && budget.Year == spentOn.Year && budget.Month == spentOn.Month)
               ?? throw new NotFoundException($"Budget for {spentOn.Year}-{spentOn.Month:00}");
    }
}
