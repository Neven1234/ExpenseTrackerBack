using System.Linq.Expressions;
using ExpenseTracker.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _dbContext;
    private readonly DbSet<T> _entities;

    public Repository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
        _entities = dbContext.Set<T>();
    }

    // Reads are untracked; GetAsync and GetByIdAsync stay tracked so callers can update or remove what they fetched.
    public async Task<IReadOnlyList<T>> GetAllAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params string[] includes)
    {
        var query = Include(_entities.AsNoTracking(), includes);

        if (predicate is not null)
            query = query.Where(predicate);

        if (orderBy is not null)
            query = orderBy(query);

        return await query.ToListAsync();
    }

    public async Task<T?> GetAsync(Expression<Func<T, bool>> predicate, params string[] includes)
    {
        return await Include(_entities, includes).FirstOrDefaultAsync(predicate);
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _entities.FindAsync(id);
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await _entities.AnyAsync(predicate);
    }

    public void Add(T entity) => _entities.Add(entity);

    public void Update(T entity) => _entities.Update(entity);

    public void Remove(T entity) => _entities.Remove(entity);

    private static IQueryable<T> Include(IQueryable<T> query, string[] includes)
    {
        return includes.Aggregate(query, (current, include) => current.Include(include));
    }
}
