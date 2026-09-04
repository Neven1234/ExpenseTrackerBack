namespace ExpenseTracker.Application.DTOs.Budgets;

public record MonthlyBudgetSummaryResponse(
    MonthlyBudgetResponse Budget,
    IReadOnlyList<CategorySpendResponse> SpendingByCategory);
