namespace CustomerOrderManagement.Domain.DTOs.Orders;

/// <summary>
/// Aggregate order statistics for a single customer.
/// </summary>
public sealed class CustomerOrderSummaryDto
{
    public Guid CustomerId { get; set; }

    public int TotalOrders { get; set; }

    public decimal TotalSpent { get; set; }

    public int PendingOrders { get; set; }

    public DateTime? LastOrderDate { get; set; }
}
