using System.Linq.Expressions;

namespace ExpenseTracker.Application.Abstractions.Persistence;

public interface IRepository<T> where T : class
{
    Task<IReadOnlyList<T>> GetAllAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params string[] includes);

    Task<T?> GetAsync(Expression<Func<T, bool>> predicate, params string[] includes);

    Task<T?> GetByIdAsync(Guid id);

    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

    void Add(T entity);

    void Update(T entity);

    void Remove(T entity);
}
