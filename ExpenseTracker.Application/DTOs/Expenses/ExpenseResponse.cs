namespace ExpenseTracker.Application.DTOs.Expenses;

public record ExpenseResponse(
    Guid Id,
    decimal Amount,
    string Note,
    DateOnly SpentOn,
    Guid CategoryId,
    string CategoryName);
