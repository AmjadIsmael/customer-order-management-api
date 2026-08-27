namespace CustomerOrderManagement.Domain.DTOs.Orders;

/// <summary>
/// A single product/quantity pair used when creating an order or adding a line item.
/// </summary>
public sealed class OrderItemCreateDto
{
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
}
