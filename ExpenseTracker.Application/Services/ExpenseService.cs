using ExpenseTracker.Application.Abstractions.Security;
using ExpenseTracker.Application.DTOs.Expenses;
using ExpenseTracker.Application.Mapping;
using ExpenseTracker.Application.Persistence;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services;

public class ExpenseService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ExpenseService(AppDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ExpenseResponse>> ListAsync(int? year, int? month, Guid? categoryId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Expenses.AsNoTracking()
            .Include(expense => expense.Category)
            .Where(expense => expense.MonthlyBudget.UserId == _currentUser.Id);

        if (year.HasValue)
            query = query.Where(expense => expense.MonthlyBudget.Year == year.Value);

        if (month.HasValue)
            query = query.Where(expense => expense.MonthlyBudget.Month == month.Value);

        if (categoryId.HasValue)
            query = query.Where(expense => expense.CategoryId == categoryId.Value);

        var expenses = await query
            .OrderByDescending(expense => expense.SpentOn)
            .ToListAsync(cancellationToken);

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

        _dbContext.Expenses.Add(expense);
        await _dbContext.SaveChangesAsync(cancellationToken);

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

        await _dbContext.SaveChangesAsync(cancellationToken);

        return expense.ToResponse(category.Name);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await FindOrThrowAsync(id, cancellationToken);

        _dbContext.Expenses.Remove(expense);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Expense> FindOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Expenses
                   .Include(expense => expense.Category)
                   .FirstOrDefaultAsync(
                       expense => expense.Id == id && expense.MonthlyBudget.UserId == _currentUser.Id,
                       cancellationToken)
               ?? throw new NotFoundException("Expense");
    }

    private async Task<Category> FindCategoryOrThrowAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        return await _dbContext.Categories.AsNoTracking()
                   .FirstOrDefaultAsync(category => category.Id == categoryId && category.UserId == _currentUser.Id, cancellationToken)
               ?? throw new NotFoundException("Category");
    }

    private async Task<MonthlyBudget> FindBudgetOrThrowAsync(DateOnly spentOn, CancellationToken cancellationToken)
    {
        return await _dbContext.MonthlyBudgets.AsNoTracking()
                   .FirstOrDefaultAsync(
                       budget => budget.UserId == _currentUser.Id && budget.Year == spentOn.Year && budget.Month == spentOn.Month,
                       cancellationToken)
               ?? throw new NotFoundException($"Budget for {spentOn.Year}-{spentOn.Month:00}");
    }
}
