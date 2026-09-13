using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Features.Orders.Commands.CreateOrder;
using OrderFlow.Application.Features.Orders.Queries.GetOrderById;
using OrderFlow.Application.Features.Orders.Queries.GetOrdersList;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Creates a new order.
    /// </summary>
    /// <param name="command">The order to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The identifier of the created order.</returns>
    /// <response code="201">Order created successfully.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var orderId = await sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = orderId },
            new { id = orderId });
    }

    /// <summary>
    /// Gets a single order with its items.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The order details.</returns>
    /// <response code="200">Order found.</response>
    /// <response code="404">Order not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDetailsDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var order = await sender.Send(new GetOrderByIdQuery(id), cancellationToken);

        return Ok(order);
    }

    /// <summary>
    /// Gets a lightweight list of all orders.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of order summaries.</returns>
    /// <response code="200">List returned.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrderSummaryDto>>> List(
        CancellationToken cancellationToken)
    {
        var orders = await sender.Send(new GetOrdersListQuery(), cancellationToken);

        return Ok(orders);
    }
}
