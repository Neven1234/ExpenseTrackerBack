namespace ExpenseTracker.Domain.Entities;

public class MonthlyBudget
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Allowance { get; set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public decimal Spent => Expenses.Sum(expense => expense.Amount);
}
