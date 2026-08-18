using Fulfillment.Application.Categories.Commands.CreateCategory;
using Fulfillment.Application.Categories.Queries.GetCategories;
using Fulfillment.Application.Common.Models;
using Fulfillment.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fulfillment.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ISender _sender;
    public CategoriesController(ISender sender) => _sender = sender;

    [HttpPost]
    [Authorize(Policy = Policies.CanManageCatalog)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryCommand command, CancellationToken ct)
    {
        var id = await _sender.Send(command, ct);
        return StatusCode(StatusCodes.Status201Created, new {id});
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _sender.Send(
            new GetCategoriesQuery(search, isActive, pageNumber, pageSize), ct));
}