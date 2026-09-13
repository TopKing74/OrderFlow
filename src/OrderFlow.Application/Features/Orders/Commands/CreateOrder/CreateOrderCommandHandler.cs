using MediatR;
using OrderFlow.Application.Common.Caching;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Features.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService)
    : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = Order.Create(
            request.CustomerName,
            request.Items.Select(item => OrderItem.Create(item.ProductName, item.Quantity, item.UnitPrice)));

        dbContext.Orders.Add(order);

        await dbContext.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync(CacheKeys.OrdersList, cancellationToken);
        await cacheService.RemoveAsync(CacheKeys.OrdersDashboard, cancellationToken);

        return order.Id;
    }
}
