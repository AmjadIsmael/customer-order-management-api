using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Persistence.Context;

namespace CustomerOrderManagement.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public ICustomerRepository Customers { get; }

    public IUserRepository Users { get; }

    public UnitOfWork(
        ApplicationDbContext context,
        ICustomerRepository customerRepository,
        IUserRepository userRepository)
    {
        _context = context;
        Customers = customerRepository;
        Users = userRepository;
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
