using ExpenseTracker.Application.DTOs.Budgets;
using ExpenseTracker.Domain.Budgeting;

namespace ExpenseTracker.Application.Mapping;

public static class BudgetMapping
{
    public static MonthlyBudgetResponse ToResponse(this BudgetBalance balance) =>
        new(
            balance.Budget.Id,
            balance.Budget.Year,
            balance.Budget.Month,
            balance.Budget.Allowance,
            balance.CarriedOver,
            balance.TotalAvailable,
            balance.Spent,
            balance.Remaining);
}
