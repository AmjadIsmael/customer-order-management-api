using CustomerOrderManagement.Domain.Entities;

namespace CustomerOrderManagement.Business.Interfaces.Persistence;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        Guid? excludedCustomerId = null,
        CancellationToken cancellationToken = default);
}
