using Fulfillment.Api.Contracts;
using Fulfillment.Application.Common.Models;
using Fulfillment.Application.Products.Commands.CreateProduct;
using Fulfillment.Application.Products.Commands.SetProductStatus;
using Fulfillment.Application.Products.Queries.GetProductById;
using Fulfillment.Application.Products.Queries.GetProducts;
using Fulfillment.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fulfillment.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;
    public ProductsController(ISender sender) => _sender = sender;

    [HttpPost]
    [Authorize(Policy = Policies.CanManageCatalog)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCommand command, CancellationToken ct)
    {
        var id = await  _sender.Send(command, ct);
        return StatusCode(StatusCodes.Status201Created, new {id});
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllProducts([FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? lowStockOnly = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _sender.Send(
            new GetProductsQuery(search, categoryId, isActive, lowStockOnly,pageNumber, pageSize), ct));
    
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken ct) 
    => Ok(await _sender.Send(new GetProductByIdQuery(id), ct));

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Policies.CanManageCatalog)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetProductStatus(Guid id, [FromBody] SetProductStatusRequest request,
        CancellationToken ct)
    {
        await _sender.Send(new SetProductStatusCommand(id, request.IsActive), ct);
        return NoContent();

    }
}