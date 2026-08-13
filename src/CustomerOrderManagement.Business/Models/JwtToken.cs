namespace CustomerOrderManagement.Business.Models;

/// <summary>
/// A signed JWT plus the UTC instant it stops being valid.
/// </summary>
public sealed record JwtToken(string Value, DateTime ExpiresAtUtc);
