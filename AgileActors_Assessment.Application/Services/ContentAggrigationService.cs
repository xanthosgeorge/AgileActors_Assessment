using AgileActors_Assessment.Application.Interfaces;
using AgileActors_Assessment.Application.Models;
using AgileActors_Assessment.Domain.Models;
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

        public ContentAggregationService(IEnumerable<IAggregationSource> sources)
        {
            _sources = sources;
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
                .Where(source => query.Source is null || source.Source == query.Source)
                .ToArray();


            // Fetch all selected external sources concurrently to reduce total response time.
            var sourceResults = await Task.WhenAll(
                selectedSources.Select(source => GetSourceResultAsync(source, cancellationToken)));

            IEnumerable<ContentItem> items = sourceResults
                .Where(result => result.IsSuccessful)
                .SelectMany(result => result.Items);

            return new AggregationResult(
                items.ToArray(),
                sourceResults);
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
