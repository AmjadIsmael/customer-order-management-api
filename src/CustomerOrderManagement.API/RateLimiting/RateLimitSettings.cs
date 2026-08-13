namespace CustomerOrderManagement.API.RateLimiting;

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public RateLimitPolicySettings Global { get; init; } = new() { PermitLimit = 100, WindowSeconds = 60 };

    public RateLimitPolicySettings Auth { get; init; } = new() { PermitLimit = 100, WindowSeconds = 60 };
}

public sealed class RateLimitPolicySettings
{
    public int PermitLimit { get; init; }

    public int WindowSeconds { get; init; }

    public int QueueLimit { get; init; }
}
