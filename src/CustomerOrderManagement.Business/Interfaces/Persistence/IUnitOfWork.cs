namespace CustomerOrderManagement.Business.Interfaces.Persistence;

public interface IUnitOfWork
{
    ICustomerRepository Customers { get; }

    IUserRepository Users { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
