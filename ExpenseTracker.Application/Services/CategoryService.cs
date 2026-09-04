using ExpenseTracker.Application.Abstractions.Security;
using ExpenseTracker.Application.DTOs.Categories;
using ExpenseTracker.Application.Mapping;
using ExpenseTracker.Application.Persistence;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services;

public class CategoryService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CategoryService(AppDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _dbContext.Categories.AsNoTracking()
            .Where(category => category.UserId == _currentUser.Id)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(category => category.ToResponse()).ToList();
    }

    public async Task<CategoryResponse> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await FindOrThrowAsync(id, cancellationToken);
        return category.ToResponse();
    }

    public async Task<CategoryResponse> CreateAsync(CategoryRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        await EnsureNameIsAvailableAsync(name, null, cancellationToken);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.Id,
            Name = name
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return category.ToResponse();
    }

    public async Task<CategoryResponse> UpdateAsync(Guid id, CategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await FindOrThrowAsync(id, cancellationToken);
        var name = request.Name.Trim();
        await EnsureNameIsAvailableAsync(name, id, cancellationToken);

        category.Name = name;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return category.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await FindOrThrowAsync(id, cancellationToken);

        if (await _dbContext.Expenses.AnyAsync(expense => expense.CategoryId == id, cancellationToken))
            throw new ConflictException("Category still has expenses logged against it.");

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> FindOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Categories
                   .FirstOrDefaultAsync(category => category.Id == id && category.UserId == _currentUser.Id, cancellationToken)
               ?? throw new NotFoundException("Category");
    }

    private async Task EnsureNameIsAvailableAsync(string name, Guid? excludedId, CancellationToken cancellationToken)
    {
        var nameIsTaken = await _dbContext.Categories.AnyAsync(
            category => category.UserId == _currentUser.Id
                && category.Name == name
                && (excludedId == null || category.Id != excludedId),
            cancellationToken);

        if (nameIsTaken)
            throw new ConflictException($"A category named '{name}' already exists.");
    }
}
