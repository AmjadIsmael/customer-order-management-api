using CustomerOrderManagement.Domain.Common;
using CustomerOrderManagement.Domain.Enums;

namespace CustomerOrderManagement.Domain.Entities;

public sealed class Customer : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public int? Age { get; set; }

    public Gender Gender { get; set; } = Gender.Unspecified;

    public ICollection<Order> Orders { get; set; } = [];

    public User? User { get; set; }
}
