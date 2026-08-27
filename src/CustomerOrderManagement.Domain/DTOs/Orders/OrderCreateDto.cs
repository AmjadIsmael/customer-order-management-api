namespace CustomerOrderManagement.Domain.DTOs.Orders;

/// <summary>
/// The details required to create a new order, including its initial line items.
/// </summary>
public sealed class OrderCreateDto
{
    public Guid CustomerId { get; set; }

    public string ShippingAddress { get; set; } = string.Empty;

    public List<OrderItemCreateDto> Items { get; set; } = [];
}
