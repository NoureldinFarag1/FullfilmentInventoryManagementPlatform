using Fulfillment.Application.Common.Models;
using Fulfillment.Application.Customers.Commands.CreateCustomer;
using Fulfillment.Application.Customers.Queries.GetCustomers;
using Fulfillment.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fulfillment.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomerController : ControllerBase
{
    private ISender _sender;
    
    public CustomerController(ISender sender) => _sender = sender;

    [HttpPost]
    [Authorize(Policy = Policies.CanManageOrders)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command, CancellationToken ct)
    {
        var id = await _sender.Send(command, ct);
        return StatusCode(StatusCodes.Status201Created, new {id});
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new GetCustomersQuery(search, pageNumber, pageSize), ct));
}