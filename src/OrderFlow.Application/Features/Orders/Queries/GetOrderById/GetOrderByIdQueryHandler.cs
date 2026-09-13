using Dapper;
using MediatR;
using OrderFlow.Application.Common.Caching;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Features.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    ICacheService cacheService)
    : IRequestHandler<GetOrderByIdQuery, OrderDetailsDto>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<OrderDetailsDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.OrderDetails(request.Id);

        var cached = await cacheService.GetAsync<OrderDetailsDto>(cacheKey, cancellationToken);

        if (cached is not null)
        {
            return cached;
        }

        const string sql = """
            SELECT [Id], [CustomerName], [Status], [TotalAmount], [CreatedAtUtc]
            FROM [Orders]
            WHERE [Id] = @Id;

            SELECT [ProductName], [Quantity], [UnitPrice], [LineTotal]
            FROM [OrderItems]
            WHERE [OrderId] = @Id;
            """;

        await using var connection = sqlConnectionFactory.CreateOpenConnection();

        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { request.Id }, cancellationToken: cancellationToken));

        var order = await multi.ReadSingleOrDefaultAsync<OrderDetailsRow>();

        if (order is null)
        {
            throw new NotFoundException(nameof(Order), request.Id);
        }

        var items = (await multi.ReadAsync<OrderDetailsItemDto>()).ToList();

        var orderDetails = new OrderDetailsDto(
            order.Id,
            order.CustomerName,
            order.Status,
            order.TotalAmount,
            order.CreatedAtUtc,
            items);

        await cacheService.SetAsync(cacheKey, orderDetails, CacheDuration, cancellationToken);

        return orderDetails;
    }
}

public sealed record OrderDetailsRow(
    Guid Id,
    string CustomerName,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc);
