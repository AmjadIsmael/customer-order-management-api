using CustomerOrderManagement.Business.Configuration;
using CustomerOrderManagement.Business.Interfaces.Services;
using CustomerOrderManagement.Business.Services;
using CustomerOrderManagement.Business.Validation.Customers;
using CustomerOrderManagement.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerOrderManagement.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusiness(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddValidatorsFromAssemblyContaining<CustomerCreateDtoValidator>();

        return services;
    }
}
