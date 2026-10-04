using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;
using OrderFlow.Api.Observability;
using OrderFlow.Application.Common.Caching;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Features.Orders.Commands.CreateOrder;
using OrderFlow.Application.Features.Orders.Queries.GetOrderById;
using OrderFlow.Application.Features.Orders.Queries.GetOrdersList;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(
    ISender sender,
    OrderFlowTelemetry telemetry,
    ICacheService cacheService,
    ILogger<OrdersController> logger) : ControllerBase
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
        using var activity = telemetry.StartCreateOrderActivity();
        var totalAmount = command.Items.Sum(item => item.Quantity * item.UnitPrice);

        try
        {
            var orderId = await sender.Send(command, cancellationToken);

            activity?.SetTag("order.id", orderId);
            activity?.SetTag("order.amount", totalAmount);

            telemetry.OrdersCreatedTotal.Add(1);
            telemetry.PendingOrdersCount.Add(1);

            logger.LogInformation("Order {OrderId} created successfully with amount {Amount}", orderId, totalAmount);

            return CreatedAtAction(
                nameof(GetById),
                new { id = orderId },
                new { id = orderId });
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            telemetry.HttpErrorsTotal.Add(1);
            logger.LogError(ex, "Error processing {OperationName}", OrderFlowTelemetry.CreateOrderOperation);
            throw;
        }
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
        using var activity = telemetry.StartGetOrderByIdActivity();
        activity?.SetTag("order.id", id);

        try
        {
            var cacheKey = CacheKeys.OrderDetails(id);
            var cached = await cacheService.GetAsync<OrderDetailsDto>(cacheKey, cancellationToken);

            string hitOrMiss;
            OrderDetailsDto order;

            if (cached is not null)
            {
                hitOrMiss = "HIT";
                order = cached;
            }
            else
            {
                hitOrMiss = "MISS";
                order = await sender.Send(new GetOrderByIdQuery(id), cancellationToken);
            }

            activity?.SetTag("cache.status", hitOrMiss);
            logger.LogInformation("Cache {CacheStatus} for Order {OrderId}", hitOrMiss, id);

            return Ok(order);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            telemetry.HttpErrorsTotal.Add(1);
            logger.LogError(ex, "Error processing {OperationName}", OrderFlowTelemetry.GetOrderByIdOperation);
            throw;
        }
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
        using var activity = telemetry.StartGetOrdersActivity();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var orders = await sender.Send(new GetOrdersListQuery(), cancellationToken);
            stopwatch.Stop();

            activity?.SetTag("query.orders_count", orders.Count);
            activity?.SetTag("query.duration_ms", stopwatch.ElapsedMilliseconds);

            logger.LogInformation(
                "Retrieved {OrderCount} orders in {ElapsedMilliseconds}ms",
                orders.Count,
                stopwatch.ElapsedMilliseconds);

            return Ok(orders);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            telemetry.HttpErrorsTotal.Add(1);
            logger.LogError(ex, "Error processing {OperationName}", OrderFlowTelemetry.GetOrdersOperation);
            throw;
        }
    }
}
