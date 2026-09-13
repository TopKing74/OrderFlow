using Dapper;
using MediatR;
using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Application.Features.Orders.Queries.GetOrdersList;

public sealed class GetOrdersListQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IRequestHandler<GetOrdersListQuery, List<OrderSummaryDto>>
{
    public async Task<List<OrderSummaryDto>> Handle(GetOrdersListQuery request, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT o.[Id],
                   o.[CustomerName],
                   o.[Status],
                   o.[TotalAmount],
                   COUNT(i.[Id]) AS [ItemCount],
                   o.[CreatedAtUtc]
            FROM [Orders] o
            LEFT JOIN [OrderItems] i ON i.[OrderId] = o.[Id]
            GROUP BY o.[Id], o.[CustomerName], o.[Status], o.[TotalAmount], o.[CreatedAtUtc]
            ORDER BY o.[CreatedAtUtc] DESC;
            """;

        await using var connection = sqlConnectionFactory.CreateOpenConnection();

        var orders = (await connection.QueryAsync<OrderSummaryDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken))).ToList();

        return orders;
    }
}
