using CustomerOrderManagement.Domain.DTOs.Orders;
using CustomerOrderManagement.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace CustomerOrderManagement.Business.Mapping;

[Mapper]
public static partial class OrderMappingExtensions
{
    [MapperIgnoreSource(nameof(Order.IsDeleted))]
    [MapperIgnoreSource(nameof(Order.Customer))]
    public static partial OrderResponseDto ToResponseDto(this Order order);

    [MapperIgnoreSource(nameof(OrderItem.IsDeleted))]
    [MapperIgnoreSource(nameof(OrderItem.IsActive))]
    [MapperIgnoreSource(nameof(OrderItem.Order))]
    [MapperIgnoreSource(nameof(OrderItem.OrderId))]
    [MapperIgnoreSource(nameof(OrderItem.CreatedDate))]
    [MapperIgnoreSource(nameof(OrderItem.CreatedBy))]
    [MapperIgnoreSource(nameof(OrderItem.UpdatedDate))]
    [MapperIgnoreSource(nameof(OrderItem.UpdatedBy))]
    public static partial OrderItemResponseDto ToResponseDto(this OrderItem item);
}
