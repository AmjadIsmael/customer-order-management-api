using CustomerOrderManagement.Domain.Entities;

namespace CustomerOrderManagement.Business.Interfaces.Persistence;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);
}
