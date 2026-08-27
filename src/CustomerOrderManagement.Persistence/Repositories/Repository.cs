using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Domain.Common;
using CustomerOrderManagement.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace CustomerOrderManagement.Persistence.Repositories;

public class Repository<T> : IRepository<T>
    where T : BaseEntity
{
    protected ApplicationDbContext Context { get; }

    protected DbSet<T> Entities { get; }

    public Repository(ApplicationDbContext context)
    {
        Context = context;
        Entities = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await Entities.FirstOrDefaultAsync(
            entity => entity.Id == id && !entity.IsDeleted,
            cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await Entities
            .AsNoTracking()
            .Where(entity => !entity.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        T entity,
        CancellationToken cancellationToken = default)
    {
        await Entities.AddAsync(entity, cancellationToken);
    }

    public void Update(T entity)
    {
        Entities.Update(entity);
    }
}
