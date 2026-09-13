using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Infrastructure.BackgroundJobs;

public sealed class PendingOrderProcessorJob(
    IServiceScopeFactory scopeFactory,
    ILogger<PendingOrderProcessorJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "PendingOrderProcessorJob started. Polling every {IntervalSeconds} seconds.",
            Interval.TotalSeconds);

        using var timer = new PeriodicTimer(Interval);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingOrdersAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Error while processing pending orders. Will retry on next tick.");
                }

                try
                {
                    if (!await timer.WaitForNextTickAsync(stoppingToken))
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            logger.LogInformation("PendingOrderProcessorJob stopped.");
        }
    }

    private async Task ProcessPendingOrdersAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var pendingOrders = await dbContext.Orders
            .Where(order => order.Status == OrderStatus.Pending)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingOrders.Count == 0)
        {
            logger.LogDebug("No pending orders to process.");
            return;
        }

        foreach (var order in pendingOrders)
        {
            order.MarkCompleted();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Marked {OrderCount} pending order(s) as {Status} (max batch size: {BatchSize}).",
            pendingOrders.Count,
            nameof(OrderStatus.Completed),
            BatchSize);
    }
}
