namespace CustomerOrderManagement.Domain.DTOs.Auth;

public sealed class LoginResponseDto
{
    public string TokenType { get; set; } = "Bearer";

    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}
