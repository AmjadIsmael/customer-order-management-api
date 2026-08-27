using CustomerOrderManagement.Domain.DTOs.Products;

namespace CustomerOrderManagement.Business.Interfaces.Services;

public interface IProductService
{
    Task<ProductResponseDto> CreateAsync(
        ProductCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<ProductResponseDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ProductResponseDto> UpdateAsync(
        Guid id,
        ProductUpdateDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
