using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Domain.Entities;
using CustomerOrderManagement.Persistence.Context;

namespace CustomerOrderManagement.Persistence.Repositories;

public sealed class ProductRepository
    : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context)
        : base(context)
    {
    }
}
