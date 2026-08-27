namespace CustomerOrderManagement.Domain.DTOs.Auth;

/// <summary>
/// Credentials submitted to <c>POST /api/v1/auth/login</c>.
/// </summary>
public sealed class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
