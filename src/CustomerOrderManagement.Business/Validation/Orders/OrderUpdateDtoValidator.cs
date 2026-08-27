using CustomerOrderManagement.Domain.DTOs.Orders;
using FluentValidation;

namespace CustomerOrderManagement.Business.Validation.Orders;

public sealed class OrderUpdateDtoValidator : AbstractValidator<OrderUpdateDto>
{
    public OrderUpdateDtoValidator()
    {
        RuleFor(dto => dto.Status)
            .IsInEnum();

        RuleFor(dto => dto.ShippingAddress)
            .NotEmpty()
            .MaximumLength(250);
    }
}
