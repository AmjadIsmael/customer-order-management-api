using CustomerOrderManagement.Domain.Enums;

namespace CustomerOrderManagement.Domain.DTOs.Auth;

/// <summary>
/// The details required to self-register as a customer via <c>POST /api/v1/auth/register</c>.
/// Creates a linked <c>Customer</c> profile and <c>User</c> login account in one call.
/// </summary>
public sealed class RegisterRequestDto
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public int? Age { get; set; }

    public Gender Gender { get; set; } = Gender.Unspecified;
}
