using CustomerOrderManagement.Domain.DTOs.Orders;
using CustomerOrderManagement.Domain.Entities;

namespace CustomerOrderManagement.Business.Interfaces.Persistence;

public interface IOrderRepository : IRepository<Order>
{

    Task<CustomerOrderSummaryDto> GetCustomerOrderSummaryAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderSearchResultDto>> SearchOrdersAsync(
        OrderSearchDto filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetStalePendingAsync(
        DateTime cutoffUtc,
        CancellationToken cancellationToken = default);
}
