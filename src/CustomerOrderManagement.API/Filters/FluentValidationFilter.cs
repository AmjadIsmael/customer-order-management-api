using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustomerOrderManagement.API.Filters;

/// <summary>
/// Runs the FluentValidation validator registered for each action argument's
/// type (if any) before the action executes, and short-circuits with the
/// standard <see cref="ValidationProblemDetails"/> 400 response on failure —
/// the FluentValidation equivalent of the automatic DataAnnotations model
/// validation that <c>[ApiController]</c> performs out of the box.
/// </summary>
public sealed class FluentValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (_serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(
                validationContext,
                context.HttpContext.RequestAborted);

            foreach (var error in result.Errors)
            {
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid && context.Controller is ControllerBase controller)
        {
            context.Result = controller.ValidationProblem(context.ModelState);
            return;
        }

        await next();
    }
}
