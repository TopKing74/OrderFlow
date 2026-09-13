using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Features.Orders.Queries.GetDashboardOrders;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Gets the dashboard read model for all orders, served from the
    /// materialized OrdersDashboard table (not the source of truth).
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The dashboard orders list.</returns>
    /// <response code="200">Dashboard orders returned.</response>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(List<OrderDashboardReadModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrderDashboardReadModel>>> Orders(
        CancellationToken cancellationToken)
    {
        var orders = await sender.Send(new GetDashboardOrdersQuery(), cancellationToken);

        return Ok(orders);
    }
}
