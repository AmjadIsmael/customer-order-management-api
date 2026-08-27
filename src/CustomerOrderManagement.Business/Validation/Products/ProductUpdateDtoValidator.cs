using CustomerOrderManagement.Domain.DTOs.Products;
using FluentValidation;

namespace CustomerOrderManagement.Business.Validation.Products;

public sealed class ProductUpdateDtoValidator : AbstractValidator<ProductUpdateDto>
{
    public ProductUpdateDtoValidator()
    {
        RuleFor(dto => dto.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(dto => dto.Description)
            .MaximumLength(1000);

        RuleFor(dto => dto.Price)
            .GreaterThan(0);

        RuleFor(dto => dto.StockQuantity)
            .GreaterThanOrEqualTo(0);
    }
}
