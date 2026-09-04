using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Domain.Budgeting;

public static class BudgetLedger
{
    public static IEnumerable<BudgetBalance> Accumulate(IEnumerable<MonthlyBudget> budgets)
    {
        var carriedOver = 0m;

        foreach (var budget in budgets.OrderBy(budget => budget.Year).ThenBy(budget => budget.Month))
        {
            var balance = new BudgetBalance(budget, carriedOver);
            carriedOver = balance.Remaining;
            yield return balance;
        }
    }
}
