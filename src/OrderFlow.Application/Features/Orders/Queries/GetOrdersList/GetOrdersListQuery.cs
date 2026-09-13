using MediatR;

namespace OrderFlow.Application.Features.Orders.Queries.GetOrdersList;

public sealed record GetOrdersListQuery : IRequest<List<OrderSummaryDto>>;

public sealed record OrderSummaryDto(
    Guid Id,
    string CustomerName,
    string Status,
    decimal TotalAmount,
    int ItemCount,
    DateTime CreatedAtUtc);
