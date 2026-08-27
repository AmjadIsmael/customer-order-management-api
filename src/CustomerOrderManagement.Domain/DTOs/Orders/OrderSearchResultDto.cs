using CustomerOrderManagement.Domain.Enums;

namespace CustomerOrderManagement.Domain.DTOs.Orders;

/// <summary>
/// A single order search result, including the customer's name.
/// </summary>
public sealed class OrderSearchResultDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public string? ShippingAddress { get; set; }

    public DateTime CreatedDate { get; set; }
}
