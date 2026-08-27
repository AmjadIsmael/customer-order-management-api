using CustomerOrderManagement.Domain.DTOs.Orders;
using FluentValidation;

namespace CustomerOrderManagement.Business.Validation.Orders;

public sealed class OrderItemCreateDtoValidator : AbstractValidator<OrderItemCreateDto>
{
    public OrderItemCreateDtoValidator()
    {
        RuleFor(dto => dto.ProductId)
            .NotEmpty();

        RuleFor(dto => dto.Quantity)
            .GreaterThan(0);
    }
}
