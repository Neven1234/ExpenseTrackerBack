using ExpenseTracker.Application.DTOs.Expenses;
using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Application.Mapping;

public static class ExpenseMapping
{
    public static ExpenseResponse ToResponse(this Expense expense, string categoryName) =>
        new(expense.Id, expense.Amount, expense.Note, expense.SpentOn, expense.CategoryId, categoryName);

    public static ExpenseResponse ToResponse(this Expense expense) =>
        expense.ToResponse(expense.Category?.Name ?? string.Empty);
}
