using CustomerOrderManagement.Business.Interfaces.Services;
using Microsoft.Extensions.Options;
using Quartz;

namespace CustomerOrderManagement.API.Jobs;

[DisallowConcurrentExecution]
public sealed class ExpireStalePendingOrdersJob : IJob
{
    private readonly IOrderService _orderService;
    private readonly ExpireStalePendingOrdersSettings _settings;
    private readonly ILogger<ExpireStalePendingOrdersJob> _logger;

    public ExpireStalePendingOrdersJob(
        IOrderService orderService,
        IOptions<JobSettings> settings,
        ILogger<ExpireStalePendingOrdersJob> logger)
    {
        _orderService = orderService;
        _settings = settings.Value.ExpireStalePendingOrders;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var threshold = TimeSpan.FromHours(_settings.PendingHours);

        var cancelledOrderIds = await _orderService.ExpireStalePendingOrdersAsync(
            threshold,
            context.CancellationToken);

        if (cancelledOrderIds.Count == 0)
        {
            _logger.LogDebug(
                "No orders have been pending for longer than {Hours}h.",
                _settings.PendingHours);
            return;
        }

        _logger.LogInformation(
            "Auto-cancelled {Count} order(s) pending for more than {Hours}h: {OrderIds}",
            cancelledOrderIds.Count,
            _settings.PendingHours,
            string.Join(", ", cancelledOrderIds));
    }
}
