using AgileActors_Assessment.Application.Interfaces;
using AgileActors_Assessment.Application.Models;
using AgileActors_Assessment.Domain.Models;

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
                selectedSources.Select(source => source.GetAsync(cancellationToken)));

            IEnumerable<ContentItem> items = sourceResults
                .Where(result => result.IsSuccessful)
                .SelectMany(result => result.Items);

            return new AggregationResult(
                items.ToArray(),
                sourceResults);
        }
    }
}
