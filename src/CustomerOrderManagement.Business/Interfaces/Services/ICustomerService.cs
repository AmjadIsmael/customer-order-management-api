using CustomerOrderManagement.Domain.DTOs.Customers;

namespace CustomerOrderManagement.Business.Interfaces.Services;

public interface ICustomerService
{
    Task<CustomerResponseDto> CreateAsync(
        CustomerCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<CustomerResponseDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<CustomerResponseDto> UpdateAsync(
        Guid id,
        CustomerUpdateDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
