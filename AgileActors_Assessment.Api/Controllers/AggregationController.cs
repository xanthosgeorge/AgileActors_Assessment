using AgileActors_Assessment.Application.Models;
using AgileActors_Assessment.Application.Services;
using AgileActors_Assessment.Domain.Enums;
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
    /// Returns aggregated content from the configured external sources,
    /// supporting filtering and sorting through query parameters.
    /// </summary>

    [HttpGet]
    public async Task<IActionResult> Get(
    [FromQuery] string? query,
    [FromQuery] ContentSource? source,
    [FromQuery] ContentType? contentType,
    [FromQuery] DateTimeOffset? from,
    [FromQuery] DateTimeOffset? to,
    [FromQuery] string sortBy = "date",
    [FromQuery] string sortDirection = "desc",
    CancellationToken cancellationToken = default)
    {
        var aggregationQuery = new AggregationQuery
        {
            Query = query,
            Source = source,
            ContentType = contentType,
            From = from,
            To = to,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var result = await _aggregationService.AggregateAsync(
            aggregationQuery,
            cancellationToken);

        return Ok(result);
    }

}