using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Domain.Budgeting;

public sealed class BudgetBalance
{
    public BudgetBalance(MonthlyBudget budget, decimal carriedOver)
    {
        Budget = budget;
        CarriedOver = carriedOver;
    }

    public MonthlyBudget Budget { get; }
    public decimal CarriedOver { get; }
    public decimal Spent => Budget.Spent;
    public decimal TotalAvailable => CarriedOver + Budget.Allowance;
    public decimal Remaining => TotalAvailable - Spent;
}
