using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Application.DTOs.Expenses;

public record ExpenseRequest
{
    [Required]
    public Guid CategoryId { get; init; }

    [Range(0.01, 1_000_000_000)]
    public decimal Amount { get; init; }

    [MaxLength(250)]
    public string Note { get; init; } = string.Empty;

    [Required]
    public DateOnly SpentOn { get; init; }
}
