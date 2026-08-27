namespace CustomerOrderManagement.Business.Models;

public sealed record JwtToken(string Value, DateTime ExpiresAtUtc);
