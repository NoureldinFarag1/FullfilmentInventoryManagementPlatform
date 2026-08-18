using Fulfillment.Application.Common.Models;
using Fulfillment.Application.Warehouses.Commands.CreateWarehouse;
using Fulfillment.Application.Warehouses.Queries.GetWarehouseById;
using Fulfillment.Application.Warehouses.Queries.GetWarehouses;
using Fulfillment.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fulfillment.Api.Controllers;

[ApiController]
[Route("api/warehouses")]
[Authorize]
public class WarehousesController : ControllerBase
{
    private readonly ISender _sender;

    public WarehousesController(ISender sender) => _sender = sender;
    
    [HttpPost]
    [Authorize(Policy = Policies.CanManageCatalog)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateWarehouseCommand command, CancellationToken ct)
    {
        var id = await _sender.Send(command, ct);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<WarehouseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _sender.Send(
            new GetWarehousesQuery(search, isActive, pageNumber, pageSize), ct));
    
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _sender.Send(new GetWarehouseByIdQuery(id), ct));
}