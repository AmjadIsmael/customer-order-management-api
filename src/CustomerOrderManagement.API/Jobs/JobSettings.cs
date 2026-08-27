namespace CustomerOrderManagement.API.Jobs;

public sealed class JobSettings
{
    public const string SectionName = "Jobs";

    public ExpireStalePendingOrdersSettings ExpireStalePendingOrders { get; init; } = new();
}

public sealed class ExpireStalePendingOrdersSettings
{
    public int PendingHours { get; init; } = 24;

    public string CronExpression { get; init; } = "0 0 * * * ?";
}
