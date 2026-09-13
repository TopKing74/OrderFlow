using MediatR;

namespace OrderFlow.Application.Features.Orders.Queries.GetDashboardOrders;

public sealed record GetDashboardOrdersQuery : IRequest<List<OrderDashboardReadModel>>;

public sealed record OrderDashboardReadModel(
    Guid Id,
    Guid OrderId,
    string CustomerName,
    int ItemCount,
    decimal TotalAmount,
    string Status,
    DateTime LastUpdatedUtc);
