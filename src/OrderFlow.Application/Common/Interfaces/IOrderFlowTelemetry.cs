using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OrderFlow.Application.Common.Interfaces;

public interface IOrderFlowTelemetry
{
    Meter Meter { get; }
    ActivitySource ActivitySource { get; }

    Counter<long> HttpRequestsTotal { get; }
    Histogram<double> HttpRequestDurationSeconds { get; }
    Counter<long> HttpErrorsTotal { get; }
    Counter<long> OrdersCreatedTotal { get; }
    UpDownCounter<int> PendingOrdersCount { get; }
    Counter<long> WorkerProcessedOrdersTotal { get; }

    Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal);
    Activity? StartCreateOrderActivity();
    Activity? StartGetOrderByIdActivity();
    Activity? StartGetOrdersActivity();
}
