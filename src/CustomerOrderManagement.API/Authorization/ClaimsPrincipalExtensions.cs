using System.Security.Claims;
using CustomerOrderManagement.Business.Configuration;
using CustomerOrderManagement.Domain.Enums;

namespace CustomerOrderManagement.API.Authorization;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetCustomerId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(CustomClaimTypes.CustomerId);
        return Guid.TryParse(value, out var customerId) ? customerId : null;
    }

    public static bool IsCustomer(this ClaimsPrincipal principal)
        => principal.IsInRole(UserRole.Customer.ToString());
}
