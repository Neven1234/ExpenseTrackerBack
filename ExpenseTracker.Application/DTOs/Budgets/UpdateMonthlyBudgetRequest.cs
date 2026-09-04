using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Application.DTOs.Budgets;

public record UpdateMonthlyBudgetRequest
{
    [Range(0, 1_000_000_000)]
    public decimal Allowance { get; init; }
}
