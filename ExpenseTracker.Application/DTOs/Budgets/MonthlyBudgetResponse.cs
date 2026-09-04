namespace ExpenseTracker.Application.DTOs.Budgets;

public record MonthlyBudgetResponse(
    Guid Id,
    int Year,
    int Month,
    decimal Allowance,
    decimal CarriedOver,
    decimal TotalAvailable,
    decimal Spent,
    decimal Remaining);
