using CustomerOrderManagement.Domain.Common;
using CustomerOrderManagement.Domain.Enums;

namespace CustomerOrderManagement.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;
}
