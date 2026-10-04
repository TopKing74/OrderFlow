using System.Diagnostics;
using System.Diagnostics.Metrics;

using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Api.Observability;

public sealed class OrderFlowTelemetry : IOrderFlowTelemetry, IDisposable
{
    public const string MeterName = "OrderFlow.Metrics";
    public const string ActivitySourceName = "OrderFlow.Observability";

    // Operation names for distributed tracing
    public const string CreateOrderOperation = "CreateOrder";
    public const string GetOrderByIdOperation = "GetOrderById";
    public const string GetOrdersOperation = "GetOrders";

    public Meter Meter { get; }
    public ActivitySource ActivitySource { get; }

    // Metrics Instruments
    public Counter<long> HttpRequestsTotal { get; }
    public Histogram<double> HttpRequestDurationSeconds { get; }
    public Counter<long> HttpErrorsTotal { get; }
    public Counter<long> OrdersCreatedTotal { get; }
    public UpDownCounter<int> PendingOrdersCount { get; }
    public Counter<long> WorkerProcessedOrdersTotal { get; }

    public OrderFlowTelemetry()
    {
        Meter = new Meter(MeterName, "1.0.0");
        ActivitySource = new ActivitySource(ActivitySourceName, "1.0.0");

        HttpRequestsTotal = Meter.CreateCounter<long>(
            name: "http_requests_total",
            unit: "{requests}",
            description: "Tracks overall request volume.");

        HttpRequestDurationSeconds = Meter.CreateHistogram<double>(
            name: "http_request_duration_seconds",
            unit: "s",
            description: "Measures request durations.");

        HttpErrorsTotal = Meter.CreateCounter<long>(
            name: "http_errors_total",
            unit: "{errors}",
            description: "Tracks error and failure counts.");

        OrdersCreatedTotal = Meter.CreateCounter<long>(
            name: "orders_created_total",
            unit: "{orders}",
            description: "Tracks successfully placed orders.");

        PendingOrdersCount = Meter.CreateUpDownCounter<int>(
            name: "pending_orders_count",
            unit: "{orders}",
            description: "Tracks current pending orders queue.");

        WorkerProcessedOrdersTotal = Meter.CreateCounter<long>(
            name: "worker_processed_orders_total",
            unit: "{orders}",
            description: "Tracks orders processed by background workers.");
    }

    /// <summary>
    /// Starts an Activity with the specified operation name under the OrderFlow.Observability source.
    /// </summary>
    public Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal)
    {
        return ActivitySource.StartActivity(operationName, kind);
    }

    public Activity? StartCreateOrderActivity() => StartActivity(CreateOrderOperation);
    public Activity? StartGetOrderByIdActivity() => StartActivity(GetOrderByIdOperation);
    public Activity? StartGetOrdersActivity() => StartActivity(GetOrdersOperation);

    public void Dispose()
    {
        Meter.Dispose();
        ActivitySource.Dispose();
    }
}
