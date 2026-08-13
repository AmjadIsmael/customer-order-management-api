using CustomerOrderManagement.Domain.DTOs.Auth;
using FluentValidation;

namespace CustomerOrderManagement.Business.Validation.Auth;

public sealed class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(dto => dto.Username)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(dto => dto.Password)
            .NotEmpty()
            .MaximumLength(100);
    }
}
