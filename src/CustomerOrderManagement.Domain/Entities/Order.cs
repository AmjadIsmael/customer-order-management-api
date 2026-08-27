using System.ComponentModel.DataAnnotations.Schema;
using CustomerOrderManagement.Domain.Common;
using CustomerOrderManagement.Domain.Enums;

namespace CustomerOrderManagement.Domain.Entities;

public sealed class Order : BaseEntity
{
    public Guid CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public string ShippingAddress { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
}
