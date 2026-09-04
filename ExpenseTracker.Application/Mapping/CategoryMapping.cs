using ExpenseTracker.Application.DTOs.Categories;
using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Application.Mapping;

public static class CategoryMapping
{
    public static CategoryResponse ToResponse(this Category category) =>
        new(category.Id, category.Name);
}
