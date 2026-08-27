using CustomerOrderManagement.Domain.Enums;

namespace CustomerOrderManagement.Domain.DTOs.Orders;

/// <summary>
/// Optional filter criteria for searching orders; any property left unset is ignored.
/// </summary>
public sealed class OrderSearchDto
{
    public Guid? CustomerId { get; set; }

    public OrderStatus? Status { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }
}
