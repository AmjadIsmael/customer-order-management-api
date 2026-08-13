using System.Net;
using CustomerOrderManagement.Business.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CustomerOrderManagement.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var (statusCode, title) = exception switch
            {
                NotFoundException => (HttpStatusCode.NotFound, "Not Found"),
                ConflictException => (HttpStatusCode.Conflict, "Conflict"),
                UnauthorizedException => (HttpStatusCode.Unauthorized, "Unauthorized"),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred."),
            };

            var problemDetails = new ProblemDetails
            {
                Status = (int)statusCode,
                Title = title,
                Detail = exception.Message,
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
