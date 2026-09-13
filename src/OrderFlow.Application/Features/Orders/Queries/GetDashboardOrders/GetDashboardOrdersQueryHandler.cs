using Dapper;
using MediatR;
using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Application.Features.Orders.Queries.GetDashboardOrders;

public sealed class GetDashboardOrdersQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IRequestHandler<GetDashboardOrdersQuery, List<OrderDashboardReadModel>>
{
    public async Task<List<OrderDashboardReadModel>> Handle(GetDashboardOrdersQuery request, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT [Id],
                   [OrderId],
                   [CustomerName],
                   [ItemCount],
                   [TotalAmount],
                   [Status],
                   [LastUpdatedUtc]
            FROM [OrdersDashboard]
            ORDER BY [LastUpdatedUtc] DESC;
            """;

        await using var connection = sqlConnectionFactory.CreateOpenConnection();

        var orders = (await connection.QueryAsync<OrderDashboardReadModel>(
            new CommandDefinition(sql, cancellationToken: cancellationToken))).ToList();

        return orders;
    }
}
