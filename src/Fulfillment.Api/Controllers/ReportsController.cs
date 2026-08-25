using Fulfillment.Application.Reports.Queries.GetOperationsSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fulfillment.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ISender _sender;
    
    public ReportsController(ISender sender) => _sender = sender;

    [HttpGet("operations-summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOperationsSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default) => Ok(await _sender.Send(new GetOperationsSummaryQuery(from, to), ct));

}