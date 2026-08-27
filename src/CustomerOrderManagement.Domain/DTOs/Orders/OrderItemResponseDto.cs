namespace CustomerOrderManagement.Domain.DTOs.Orders;

/// <summary>
/// A single order line item as returned by the API.
/// </summary>
public sealed class OrderItemResponseDto
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}
