using CustomerOrderManagement.Business.Exceptions;
using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Business.Interfaces.Services;
using CustomerOrderManagement.Business.Mapping;
using CustomerOrderManagement.Domain.DTOs.Products;

namespace CustomerOrderManagement.Business.Services;

public sealed class ProductService : IProductService
{
    private const string SystemUser = "system";

    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductResponseDto> CreateAsync(
        ProductCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var product = dto.ToEntity();
        product.CreatedDate = DateTime.UtcNow;
        product.CreatedBy = SystemUser;

        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToResponseDto();
    }

    public async Task<ProductResponseDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Product '{id}' was not found.");

        return product.ToResponseDto();
    }

    public async Task<IReadOnlyList<ProductResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Products.GetAllAsync(cancellationToken);

        return products
            .Select(product => product.ToResponseDto())
            .ToList();
    }

    public async Task<ProductResponseDto> UpdateAsync(
        Guid id,
        ProductUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Product '{id}' was not found.");

        dto.ApplyTo(product);
        product.UpdatedDate = DateTime.UtcNow;
        product.UpdatedBy = SystemUser;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToResponseDto();
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Product '{id}' was not found.");

        product.IsDeleted = true;
        product.IsActive = false;
        product.UpdatedDate = DateTime.UtcNow;
        product.UpdatedBy = SystemUser;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
