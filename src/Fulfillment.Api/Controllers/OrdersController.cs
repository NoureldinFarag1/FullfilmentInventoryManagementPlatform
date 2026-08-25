using Fulfillment.Api.Contracts;
using Fulfillment.Application.Common.Models;
using Fulfillment.Application.Orders.Commands.AddOrderItem;
using Fulfillment.Application.Orders.Commands.CancelOrder;
using Fulfillment.Application.Orders.Commands.CompleteOrder;
using Fulfillment.Application.Orders.Commands.ConfirmOrder;
using Fulfillment.Application.Orders.Commands.CreateOrder;
using Fulfillment.Application.Orders.Queries.GetOrderById;
using Fulfillment.Application.Orders.Queries.GetOrderHistory;
using Fulfillment.Application.Orders.Queries.GetOrders;
using Fulfillment.Domain.Enums;
using Fulfillment.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fulfillment.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender) => _sender = sender;

    [HttpPost]
    [Authorize(Policy = Policies.CanManageOrders)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var id = await _sender.Send(
            new CreateOrderCommand(request.CustomerId, request.WarehouseId, idempotencyKey), ct);

        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    [HttpPost("{id:guid}/items")]
    [Authorize(Policy = Policies.CanManageOrders)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddItem(
        Guid id, [FromBody] AddOrderItemRequest request, CancellationToken ct)
    {
        await _sender.Send(new AddOrderItemCommand(id, request.ProductId, request.Quantity), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = Policies.CanProcessOrders)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        await _sender.Send(new ConfirmOrderCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Policies.CanProcessOrders)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new CompleteOrderCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Policies.CanManageOrders)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelOrderRequest? request, CancellationToken ct)
    {
        await _sender.Send(new CancelOrderCommand(id, request?.Reason), ct);
        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] string? search = null,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new GetOrdersQuery(
            search, status, customerId, warehouseId, from, to,
            sortBy, sortDescending, pageNumber, pageSize), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken ct)
        => Ok(await _sender.Send(new GetOrderByIdQuery(id), ct));
    
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(OrderHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken ct)
        => Ok(await _sender.Send(new GetOrderHistoryQuery(id), ct));
}