using CustomerOrderManagement.Domain.DTOs.Orders;
using FluentValidation;

namespace CustomerOrderManagement.Business.Validation.Orders;

public sealed class OrderCreateDtoValidator : AbstractValidator<OrderCreateDto>
{
    public OrderCreateDtoValidator()
    {
        RuleFor(dto => dto.CustomerId)
            .NotEmpty();

        RuleFor(dto => dto.ShippingAddress)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(dto => dto.Items)
            .NotEmpty()
            .WithMessage("An order must contain at least one item.")
            .Must(items => items.Select(item => item.ProductId).Distinct().Count() == items.Count)
            .WithMessage("An order cannot contain the same product more than once.");

        RuleForEach(dto => dto.Items)
            .SetValidator(new OrderItemCreateDtoValidator());
    }
}
