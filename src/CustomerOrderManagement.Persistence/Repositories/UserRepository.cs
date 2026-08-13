using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Domain.Entities;
using CustomerOrderManagement.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace CustomerOrderManagement.Persistence.Repositories;

public sealed class UserRepository
    : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Trim().ToUpperInvariant();

        return await Context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user =>
                    !user.IsDeleted &&
                    user.Username.ToUpper() == normalizedUsername,
                cancellationToken);
    }
}
