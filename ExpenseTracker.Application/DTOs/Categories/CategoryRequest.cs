using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Application.DTOs.Categories;

public record CategoryRequest
{
    [Required, MaxLength(60)]
    public string Name { get; init; } = string.Empty;
}
