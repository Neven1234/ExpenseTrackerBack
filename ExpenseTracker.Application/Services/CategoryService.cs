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
    private readonly IRepository<Category> _categories;
    private readonly IRepository<Expense> _expenses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CategoryService(
        IRepository<Category> categories,
        IRepository<Expense> expenses,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _categories = categories;
        _expenses = expenses;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categories.GetAllAsync(
            category => category.UserId == _currentUser.Id,
            query => query.OrderBy(category => category.Name));

        return categories.Select(category => category.ToResponse()).ToList();
    }

    public async Task<CategoryResponse> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await FindOrThrowAsync(id);
        return category.ToResponse();
    }

    public async Task<CategoryResponse> CreateAsync(CategoryRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        await EnsureNameIsAvailableAsync(name, null);

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
        var category = await FindOrThrowAsync(id);
        var name = request.Name.Trim();
        await EnsureNameIsAvailableAsync(name, id);

        category.Name = name;
        _categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await FindOrThrowAsync(id);

        if (await _expenses.AnyAsync(expense => expense.CategoryId == id))
            throw new ConflictException("Category still has expenses logged against it.");

        _categories.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> FindOrThrowAsync(Guid id)
    {
        return await _categories.GetAsync(category => category.Id == id && category.UserId == _currentUser.Id)
            ?? throw new NotFoundException("Category");
    }

    private async Task EnsureNameIsAvailableAsync(string name, Guid? excludedId)
    {
        var nameIsTaken = await _categories.AnyAsync(
            category => category.UserId == _currentUser.Id
                && category.Name == name
                && (excludedId == null || category.Id != excludedId));

        if (nameIsTaken)
            throw new ConflictException($"A category named '{name}' already exists.");
    }
}
