using CustomerOrderManagement.Domain.DTOs.Customers;
using FluentValidation;

namespace CustomerOrderManagement.Business.Validation.Customers;

public sealed class CustomerCreateDtoValidator : AbstractValidator<CustomerCreateDto>
{
    public CustomerCreateDtoValidator()
    {
        RuleFor(dto => dto.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(dto => dto.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(dto => dto.Email)
            .NotEmpty()
            .MaximumLength(256)
            .EmailAddress();

        RuleFor(dto => dto.PhoneNumber)
            .MaximumLength(20);

        RuleFor(dto => dto.Address)
            .MaximumLength(250);

        RuleFor(dto => dto.Age)
            .InclusiveBetween(0, 150)
            .When(dto => dto.Age.HasValue);
    }
}
