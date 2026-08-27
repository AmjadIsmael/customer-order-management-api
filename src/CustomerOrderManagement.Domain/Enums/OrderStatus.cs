namespace CustomerOrderManagement.Domain.Enums;

/// <summary>
/// The lifecycle state of an order. Serialized as its underlying integer value
/// (Pending = 0, Confirmed = 1, Shipped = 2, Delivered = 3, Cancelled = 4).
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}
