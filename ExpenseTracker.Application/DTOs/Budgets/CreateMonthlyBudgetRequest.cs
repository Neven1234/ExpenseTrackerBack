using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Application.DTOs.Budgets;

public record CreateMonthlyBudgetRequest
{
    [Range(2000, 2100)]
    public int Year { get; init; }

    [Range(1, 12)]
    public int Month { get; init; }

    [Range(0, 1_000_000_000)]
    public decimal Allowance { get; init; }
}
