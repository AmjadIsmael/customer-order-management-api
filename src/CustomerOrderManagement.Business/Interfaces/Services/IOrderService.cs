using CustomerOrderManagement.Domain.DTOs.Orders;

namespace CustomerOrderManagement.Business.Interfaces.Services;

public interface IOrderService
{
    Task<OrderResponseDto> CreateAsync(
        OrderCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<OrderResponseDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<OrderResponseDto> UpdateAsync(
        Guid id,
        OrderUpdateDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OrderResponseDto> AddItemAsync(
        Guid orderId,
        OrderItemCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<OrderResponseDto> RemoveItemAsync(
        Guid orderId,
        Guid itemId,
        CancellationToken cancellationToken = default);

    Task<CustomerOrderSummaryDto> GetCustomerOrderSummaryAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderSearchResultDto>> SearchOrdersAsync(
        OrderSearchDto filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ExpireStalePendingOrdersAsync(
        TimeSpan pendingThreshold,
        CancellationToken cancellationToken = default);
}
