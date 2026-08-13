using CustomerOrderManagement.Domain.Common;

namespace CustomerOrderManagement.Business.Interfaces.Persistence;

public interface IRepository<T>
    where T : BaseEntity
{
    Task<T?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        T entity,
        CancellationToken cancellationToken = default);

    void Update(T entity);
}
