using MediatR;

namespace OrderFlow.Application.Features.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderDetailsDto>;

public sealed record OrderDetailsDto(
    Guid Id,
    string CustomerName,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    List<OrderDetailsItemDto> Items);

public sealed record OrderDetailsItemDto(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
