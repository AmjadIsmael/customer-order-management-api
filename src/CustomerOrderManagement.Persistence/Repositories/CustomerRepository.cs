using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Domain.Entities;
using CustomerOrderManagement.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace CustomerOrderManagement.Persistence.Repositories;

public sealed class CustomerRepository
    : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<Customer?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        return await Context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                customer =>
                    !customer.IsDeleted &&
                    customer.Email.ToUpper() == normalizedEmail,
                cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        Guid? excludedCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        var query = Context.Customers
            .AsNoTracking()
            .Where(customer =>
                !customer.IsDeleted &&
                customer.Email.ToUpper() == normalizedEmail);

        if (excludedCustomerId.HasValue)
        {
            query = query.Where(
                customer => customer.Id != excludedCustomerId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
