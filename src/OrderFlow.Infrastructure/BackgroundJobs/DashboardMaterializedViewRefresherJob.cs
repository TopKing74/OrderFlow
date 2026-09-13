using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderFlow.Domain.Enums;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Infrastructure.Persistence.ReadModels;

namespace OrderFlow.Infrastructure.BackgroundJobs;

public sealed class DashboardMaterializedViewRefresherJob(
    IServiceScopeFactory scopeFactory,
    ILogger<DashboardMaterializedViewRefresherJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "DashboardMaterializedViewRefresherJob started. Refreshing every {IntervalSeconds} seconds.",
            Interval.TotalSeconds);

        using var timer = new PeriodicTimer(Interval);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshDashboardAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Error while refreshing the dashboard read model. Will retry on next tick.");
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
            logger.LogInformation("DashboardMaterializedViewRefresherJob stopped.");
        }
    }

    private async Task RefreshDashboardAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var sourceOrders = await dbContext.Orders
            .AsNoTracking()
            .Select(order => new
            {
                order.Id,
                order.CustomerName,
                ItemCount = order.Items.Count,
                order.TotalAmount,
                order.Status,
                order.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var existingViews = await dbContext.OrdersDashboard
            .ToDictionaryAsync(view => view.OrderId, cancellationToken);

        var added = 0;
        var updated = 0;
        var removed = 0;
        var refreshedAtUtc = DateTime.UtcNow;

        foreach (var order in sourceOrders)
        {
            if (existingViews.TryGetValue(order.Id, out var existing))
            {
                var status = order.Status.ToString();

                if (existing.CustomerName != order.CustomerName
                    || existing.ItemCount != order.ItemCount
                    || existing.TotalAmount != order.TotalAmount
                    || existing.Status != status)
                {
                    existing.CustomerName = order.CustomerName;
                    existing.ItemCount = order.ItemCount;
                    existing.TotalAmount = order.TotalAmount;
                    existing.Status = status;
                    existing.LastUpdatedUtc = refreshedAtUtc;
                    updated++;
                }
            }
            else
            {
                dbContext.OrdersDashboard.Add(new OrderDashboardView
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    CustomerName = order.CustomerName,
                    ItemCount = order.ItemCount,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status.ToString(),
                    LastUpdatedUtc = refreshedAtUtc
                });
                added++;
            }
        }

        var sourceOrderIds = sourceOrders.Select(order => order.Id).ToHashSet();

        foreach (var existing in existingViews.Values.Where(view => !sourceOrderIds.Contains(view.OrderId)))
        {
            dbContext.OrdersDashboard.Remove(existing);
            removed++;
        }

        if (added == 0 && updated == 0 && removed == 0)
        {
            logger.LogDebug("Dashboard read model is already up to date ({OrderCount} orders).", sourceOrders.Count);
            return;
        }

        var savedChanges = await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Dashboard read model refreshed: {Added} added, {Updated} updated, {Removed} removed ({OrderCount} orders, {SavedChanges} database changes).",
            added,
            updated,
            removed,
            sourceOrders.Count,
            savedChanges);
    }
}
