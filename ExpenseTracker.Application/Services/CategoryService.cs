using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Application.Abstractions.Security;
using ExpenseTracker.Application.DTOs.Categories;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Application.Mapping;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Exceptions;

namespace ExpenseTracker.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CategoryService(ICategoryRepository categories, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categories.ListForUserAsync(_currentUser.Id, cancellationToken);
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

        _categories.Add(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category.ToResponse();
    }

    public async Task<CategoryResponse> UpdateAsync(Guid id, CategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await FindOrThrowAsync(id, cancellationToken);
        var name = request.Name.Trim();
        await EnsureNameIsAvailableAsync(name, id, cancellationToken);

        category.Name = name;
        _categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await FindOrThrowAsync(id, cancellationToken);

        if (await _categories.HasExpensesAsync(id, cancellationToken))
            throw new ConflictException("Category still has expenses logged against it.");

        _categories.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> FindOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _categories.GetForUserAsync(id, _currentUser.Id, cancellationToken)
            ?? throw new NotFoundException("Category");
    }

    private async Task EnsureNameIsAvailableAsync(string name, Guid? excludedId, CancellationToken cancellationToken)
    {
        if (await _categories.NameExistsAsync(_currentUser.Id, name, excludedId, cancellationToken))
            throw new ConflictException($"A category named '{name}' already exists.");
    }
}
