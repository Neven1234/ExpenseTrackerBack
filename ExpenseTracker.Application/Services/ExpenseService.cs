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
    private readonly IExpenseRepository _expenses;
    private readonly ICategoryRepository _categories;
    private readonly IMonthlyBudgetRepository _budgets;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ExpenseService(
        IExpenseRepository expenses,
        ICategoryRepository categories,
        IMonthlyBudgetRepository budgets,
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
        var expenses = await _expenses.ListForUserAsync(_currentUser.Id, year, month, categoryId, cancellationToken);
        return expenses.Select(expense => expense.ToResponse()).ToList();
    }

    public async Task<ExpenseResponse> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await FindOrThrowAsync(id, cancellationToken);
        return expense.ToResponse();
    }

    public async Task<ExpenseResponse> CreateAsync(ExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var category = await FindCategoryOrThrowAsync(request.CategoryId, cancellationToken);
        var budget = await FindBudgetOrThrowAsync(request.SpentOn, cancellationToken);

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
        var expense = await FindOrThrowAsync(id, cancellationToken);
        var category = await FindCategoryOrThrowAsync(request.CategoryId, cancellationToken);
        var budget = await FindBudgetOrThrowAsync(request.SpentOn, cancellationToken);

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
        var expense = await FindOrThrowAsync(id, cancellationToken);

        _expenses.Remove(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Expense> FindOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _expenses.GetForUserAsync(id, _currentUser.Id, cancellationToken)
            ?? throw new NotFoundException("Expense");
    }

    private async Task<Category> FindCategoryOrThrowAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        return await _categories.GetForUserAsync(categoryId, _currentUser.Id, cancellationToken)
            ?? throw new NotFoundException("Category");
    }

    private async Task<MonthlyBudget> FindBudgetOrThrowAsync(DateOnly spentOn, CancellationToken cancellationToken)
    {
        return await _budgets.GetForUserAsync(_currentUser.Id, spentOn.Year, spentOn.Month, cancellationToken)
            ?? throw new NotFoundException($"Budget for {spentOn.Year}-{spentOn.Month:00}");
    }
}
