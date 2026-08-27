using CustomerOrderManagement.Domain.Enums;

namespace CustomerOrderManagement.Domain.DTOs.Customers;

/// <summary>
/// The full set of editable fields for an existing customer.
/// </summary>
public sealed class CustomerUpdateDto
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public int? Age { get; set; }

    public Gender Gender { get; set; } = Gender.Unspecified;
}
