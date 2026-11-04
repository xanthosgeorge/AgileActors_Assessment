using AgileActors_Assessment.Application.Interfaces;
using AgileActors_Assessment.Application.Models;
using AgileActors_Assessment.Application.Statistics;
using AgileActors_Assessment.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text.Json;

namespace AgileActors_Assessment.Application.Services
{


    /// <summary>
    /// Aggregates content from the configured external sources and applies
    /// filtering and sorting rules to the combined result.
    /// </summary>
    public sealed class ContentAggregationService
    {
        private readonly IEnumerable<IAggregationSource> _sources;
        private readonly IMemoryCache _cache;
        private readonly RequestStatisticsService _statistics;

        public ContentAggregationService(
     IEnumerable<IAggregationSource> sources,
     IMemoryCache cache,
     RequestStatisticsService statistics)
        {
            _sources = sources;
            _cache = cache;
            _statistics = statistics;
        }



        /// <summary>
        /// Retrieves content from the selected sources concurrently and combines
        /// the successful results into a single response.
        /// </summary>

        public async Task<AggregationResult> AggregateAsync(
      AggregationQuery query,
      CancellationToken cancellationToken = default)
        {
            var selectedSources = _sources
                .Where(source =>
                    query.Source is null ||
                    source.Source == query.Source)
                .ToArray();

            var sourceResults = await Task.WhenAll(
                        selectedSources.Select(source =>
                            GetCachedSourceAsync(source, cancellationToken)));

            IEnumerable<ContentItem> items = sourceResults
                .Where(result => result.IsSuccessful)
                .SelectMany(result => result.Items);

            if (!string.IsNullOrWhiteSpace(query.Query))
            {
                items = items.Where(item =>
                    item.Title.Contains(
                        query.Query,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (query.ContentType is not null)
            {
                items = items.Where(item =>
                    item.ContentType == query.ContentType);
            }

            if (query.From is not null)
            {
                items = items.Where(item =>
                    item.CreatedAt >= query.From.Value);
            }

            if (query.To is not null)
            {
                items = items.Where(item =>
                    item.CreatedAt <= query.To.Value);
            }

            var ascending = query.SortDirection.Equals(
                "asc",
                StringComparison.OrdinalIgnoreCase);

            items = query.SortBy.ToLowerInvariant() switch
            {
                "title" => ascending
                    ? items.OrderBy(item => item.Title)
                    : items.OrderByDescending(item => item.Title),

                _ => ascending
                    ? items.OrderBy(item => item.CreatedAt)
                    : items.OrderByDescending(item => item.CreatedAt)
            };

            return new AggregationResult(
                items.ToArray(),
                sourceResults);
        }





        private async Task<SourceResult> GetCachedSourceAsync(
            IAggregationSource source,
            CancellationToken cancellationToken)
        {
            var cacheKey = $"aggregation:{source.Source}";

            if (_cache.TryGetValue(cacheKey, out SourceResult? cachedResult)
                && cachedResult is not null)
            {
                return cachedResult;
            }

            var stopwatch = Stopwatch.StartNew();

            SourceResult result;

            try
            {
                result = await GetSourceResultAsync(
                    source, cancellationToken);
            }
            finally
            {
                stopwatch.Stop();
                _statistics.Record(
                    source.Source,
                    stopwatch.ElapsedMilliseconds);
            }

            if (result.IsSuccessful)
            {
                _cache.Set(
                    cacheKey,
                    result,
                    TimeSpan.FromSeconds(60));
            }

            return result;
        }


        private static async Task<SourceResult> GetSourceResultAsync(
            IAggregationSource source,
            CancellationToken cancellationToken)
        {
            try
            {
                return await source.GetAsync(cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                var message = exception.StatusCode is { } statusCode
                    ? $"External API returned HTTP {(int)statusCode} ({statusCode})."
                    : "External API request failed.";

                return new SourceResult(source.Source, [], false, message);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new SourceResult(source.Source, [], false, "External API request timed out.");
            }
            catch (JsonException)
            {
                return new SourceResult(source.Source, [], false, "External API returned invalid JSON.");
            }
        }
    }
}
