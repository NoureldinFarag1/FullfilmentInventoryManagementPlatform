using Fulfillment.Application.Common.Models;
using Fulfillment.Application.Inventory.Commands.AdjustStock;
using Fulfillment.Application.Inventory.Commands.CreateInventoryItem;
using Fulfillment.Application.Inventory.Queries.GetStockMovements;
using Fulfillment.Application.Inventory.Queries.GetWarehouseInventory;
using Fulfillment.Domain.Enums;
using Fulfillment.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fulfillment.Api.Controllers;


[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly ISender _sender;

    public InventoryController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("items")]
    [Authorize(Policy = Policies.CanAdjustStock)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateItem(
        [FromBody] CreateInventoryItemCommand command, CancellationToken ct)
    {
        var id = await _sender.Send(command, ct);
        return CreatedAtAction(nameof(CreateItem), new { id }, new { id });
    }

    [HttpPost("items/{id:guid}/adjustments")]
    [Authorize(Policy = Policies.CanAdjustStock)]
    [ProducesResponseType(typeof(StockAdjustmentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AdjustStock(
        Guid id, [FromBody] AdjustStockRequest request, CancellationToken ct)
    {
        var result = _sender.Send(new AdjustStockCommand(id, request.Delta, request.Type, request.Reason), ct);
        
        return Ok(result);
    }

    [HttpGet("Warehouses/{warehouseId:guid}/products")]
    [ProducesResponseType(typeof(PaginatedList<WarehouseInventoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarehouseInventory(
        Guid warehouseId, [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) 
        => Ok(await _sender.Send(
            new  GetWarehouseInventoryQuery(warehouseId, search, pageNumber, pageSize), ct)); 
    
    [HttpGet("movements")]
    [ProducesResponseType(typeof(PaginatedList<StockMovementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMovements(
        [FromQuery] Guid? ProductId = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? inventoryItemId = null,
        [FromQuery] Guid? performedByUserId = null,
        [FromQuery] MovementType? type = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    => Ok(await _sender.Send(new GetStockMovementsQuery(
        ProductId, warehouseId, inventoryItemId, performedByUserId,
        type, from, to, pageNumber, pageSize), ct));
}

public record AdjustStockRequest(int Delta, MovementType Type, string? Reason);