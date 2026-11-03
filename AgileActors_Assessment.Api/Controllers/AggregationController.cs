using AgileActors_Assessment.Application.Models;
using AgileActors_Assessment.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgileActors_Assessment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AggregationController : ControllerBase
{
    private readonly ContentAggregationService _aggregationService;

    public AggregationController(ContentAggregationService aggregationService)
    {
        _aggregationService = aggregationService;
    }

    /// <summary>
    /// Returns aggregated content from the configured external sources.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] AggregationQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _aggregationService.AggregateAsync(
            query,
            cancellationToken);

        return Ok(result);
    }
}