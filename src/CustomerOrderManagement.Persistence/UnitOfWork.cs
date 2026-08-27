using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Persistence.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace CustomerOrderManagement.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    public ICustomerRepository Customers { get; }

    public IUserRepository Users { get; }

    public IProductRepository Products { get; }

    public IOrderRepository Orders { get; }

    public UnitOfWork(
        ApplicationDbContext context,
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository)
    {
        _context = context;
        Customers = customerRepository;
        Users = userRepository;
        Products = productRepository;
        Orders = orderRepository;
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
