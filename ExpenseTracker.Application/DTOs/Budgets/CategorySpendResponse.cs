namespace ExpenseTracker.Application.DTOs.Budgets;

public record CategorySpendResponse(Guid CategoryId, string CategoryName, decimal Amount);
