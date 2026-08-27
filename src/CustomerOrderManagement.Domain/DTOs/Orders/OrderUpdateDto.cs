using CustomerOrderManagement.Domain.Enums;

namespace CustomerOrderManagement.Domain.DTOs.Orders;

/// <summary>
/// The editable fields for an existing order: its status and shipping address.
/// </summary>
public sealed class OrderUpdateDto
{
    public OrderStatus Status { get; set; }

    public string ShippingAddress { get; set; } = string.Empty;
}
