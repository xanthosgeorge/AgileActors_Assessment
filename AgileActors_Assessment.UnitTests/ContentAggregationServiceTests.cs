using AgileActors_Assessment.Application.Interfaces;
using AgileActors_Assessment.Application.Models;
using AgileActors_Assessment.Application.Services;
using AgileActors_Assessment.Application.Statistcs;
using AgileActors_Assessment.Domain.Enums;
using AgileActors_Assessment.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AgileActors_Assessment.UnitTests
{
    public sealed class ContentAggregationServiceTests
    {

        /// <summary>
        /// Verifies that the aggregation service filters content items
        /// by title using a case-insensitive query.
        /// </summary>
        /// <remarks>
        /// Ensures that only matching items are returned,
        /// without making real HTTP requests.
        /// </remarks>


        [Fact]
        public async Task AggregateAsync_WhenQueryProvided_FiltersByTitle()
        {
            // Arrange
            var items = new[]
            {
            new ContentItem(
                "1",
                "Introduction to Python",
                "https://example.com/1",
                "George",
                DateTimeOffset.UtcNow,
                ContentSource.StackExchange,
                ContentType.Question),

            new ContentItem(
                "2",
                "Introduction to C#",
                "https://example.com/2",
                "George",
                DateTimeOffset.UtcNow,
                ContentSource.StackExchange,
                ContentType.Question)
        };

            var source = new FakeAggregationSource(
                ContentSource.StackExchange,
                items);

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var statistics = new RequestStatisticsService();

            var service = new ContentAggregationService(
                new[] { source },
                cache,
                statistics);

            // Act
            var result = await service.AggregateAsync(
                             new AggregationQuery { Query = "python" },
                             TestContext.Current.CancellationToken);

            // Assert
            var matchingItem = Assert.Single(result.Items);

            Assert.Equal(
                "Introduction to Python",
                matchingItem.Title);
        }



        /// <summary>
        /// Test implementation of IAggregationSource that returns
        /// predefined content without performing HTTP requests.
        /// </summary>
        /// <remarks>
        /// Tracks invocation count to verify provider selection
        /// and caching behavior during unit tests.
        /// </remarks>

        private sealed class FakeAggregationSource : IAggregationSource
        {
            private readonly IReadOnlyCollection<ContentItem> _items;
            public int CallCount { get; private set; }

            public ContentSource Source { get; }

            public FakeAggregationSource(
                ContentSource source,
                IReadOnlyCollection<ContentItem> items)
            {
                Source = source;
                _items = items;
            }

            public Task<SourceResult> GetAsync(
                CancellationToken cancellationToken = default)
            {
                CallCount++;
                return Task.FromResult(
                    new SourceResult(Source, _items, true));
            }
        }



        /// <summary>
        /// Verifies that specifying a content source limits aggregation
        /// to the selected external API provider.
        /// </summary>
        /// <remarks>
        /// Ensures that the selected provider is called exactly once
        /// and that unrelated providers are never invoked.
        /// </remarks>



        [Fact]
        public async Task AggregateAsync_WhenSourceSpecified_OnlyCallsSelectedSource()
        {
            // Arrange
            var githubItem = new ContentItem(
                "1",
                "My .NET Repository",
                "https://github.com/example/repo",
                "George",
                DateTimeOffset.UtcNow,
                ContentSource.GitHub,
                ContentType.Repository);

            var githubSource = new FakeAggregationSource(
                ContentSource.GitHub,
                new[] { githubItem });

            var stackExchangeSource = new FakeAggregationSource(
                ContentSource.StackExchange,
                Array.Empty<ContentItem>());

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var statistics = new RequestStatisticsService();

            var service = new ContentAggregationService(
                new IAggregationSource[] { githubSource, stackExchangeSource },
                cache,
                statistics);

            // Act
            var result = await service.AggregateAsync(
                new AggregationQuery { Source = ContentSource.GitHub },
                TestContext.Current.CancellationToken);

            // Assert
            var item = Assert.Single(result.Items);
            Assert.Equal(ContentSource.GitHub, item.Source);

            Assert.Single(result.Sources);
            Assert.Equal(ContentSource.GitHub, result.Sources.Single().Source);

            Assert.Equal(1, githubSource.CallCount);
            Assert.Equal(0, stackExchangeSource.CallCount);
        }


        /// <summary>
        /// Verifies that aggregation filters content items
        /// by the specified content type.
        /// </summary>
        /// <remarks>
        /// Ensures that only items matching the requested
        /// ContentType are included in the final result.
        /// </remarks>
        [Fact]
        public async Task AggregateAsync_WhenContentTypeSpecified_FiltersItems()
        {
            // Arrange
            var items = new[]
            {
        new ContentItem(
            "1",
            "C# Question",
            "https://example.com/question",
            "George",
            DateTimeOffset.UtcNow,
            ContentSource.StackExchange,
            ContentType.Question),

        new ContentItem(
            "2",
            ".NET Repository",
            "https://example.com/repository",
            "George",
            DateTimeOffset.UtcNow,
            ContentSource.GitHub,
            ContentType.Repository)
    };

            var source = new FakeAggregationSource(
                ContentSource.StackExchange,
                items);

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var statistics = new RequestStatisticsService();

            var service = new ContentAggregationService(
                new[] { source },
                cache,
                statistics);

            // Act
            var result = await service.AggregateAsync(
                new AggregationQuery
                {
                    ContentType = ContentType.Question
                },
                TestContext.Current.CancellationToken);

            // Assert
            var item = Assert.Single(result.Items);

            Assert.Equal(ContentType.Question, item.ContentType);
            Assert.Equal("C# Question", item.Title);
        }





        /// <summary>
        /// Verifies that aggregation filters items by creation date
        /// within the specified inclusive date range.
        /// </summary>
        /// <remarks>
        /// Ensures that items before From or after To are excluded,
        /// while items on the boundaries are included.
        /// </remarks>
        [Fact]
        public async Task AggregateAsync_WhenDateRangeSpecified_FiltersByCreatedAt()
        {
            // Arrange
            var items = new[]
            {
        new ContentItem(
            "1", "Before Range", "https://example.com/1",
            "George", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ContentSource.StackExchange, ContentType.Question),

        new ContentItem(
            "2", "At Start", "https://example.com/2",
            "George", new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            ContentSource.StackExchange, ContentType.Question),

        new ContentItem(
            "3", "Inside Range", "https://example.com/3",
            "George", new DateTimeOffset(2026, 2, 15, 0, 0, 0, TimeSpan.Zero),
            ContentSource.StackExchange, ContentType.Question),

        new ContentItem(
            "4", "At End", "https://example.com/4",
            "George", new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            ContentSource.StackExchange, ContentType.Question),

        new ContentItem(
            "5", "After Range", "https://example.com/5",
            "George", new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            ContentSource.StackExchange, ContentType.Question)
    };

            var source = new FakeAggregationSource(
                ContentSource.StackExchange, items);

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var statistics = new RequestStatisticsService();

            var service = new ContentAggregationService(
                new[] { source }, cache, statistics);

            // Act
            var result = await service.AggregateAsync(
                new AggregationQuery
                {
                    From = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
                    To = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)
                },
                TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(3, result.Items.Count);

            Assert.Contains(result.Items, item => item.Id == "2");
            Assert.Contains(result.Items, item => item.Id == "3");
            Assert.Contains(result.Items, item => item.Id == "4");

            Assert.DoesNotContain(result.Items, item => item.Id == "1");
            Assert.DoesNotContain(result.Items, item => item.Id == "5");
        }




        /// <summary>
        /// Verifies that aggregation sorts content items
        /// by title in ascending order.
        /// </summary>
        /// <remarks>
        /// Ensures that sorting is applied to the final aggregated
        /// results, regardless of the source's original item order.
        /// </remarks>
        [Fact]
        public async Task AggregateAsync_WhenSortingByTitleAscending_ReturnsOrderedItems()
        {
            // Arrange
            var items = new[]
            {
        new ContentItem(
            "1", "Zebra", "https://example.com/1", "George",
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ContentSource.StackExchange, ContentType.Question),

        new ContentItem(
            "2", "Apple", "https://example.com/2", "George",
            new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero),
            ContentSource.StackExchange, ContentType.Question),

        new ContentItem(
            "3", "Microsoft", "https://example.com/3", "George",
            new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero),
            ContentSource.StackExchange, ContentType.Question)
    };

            var source = new FakeAggregationSource(ContentSource.StackExchange, items);

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var statistics = new RequestStatisticsService();

            var service = new ContentAggregationService(
                new[] { source }, cache, statistics);

            // Act
            var result = await service.AggregateAsync(
                new AggregationQuery
                {
                    SortBy = "title",
                    SortDirection = "asc"
                },
                TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                new[] { "Apple", "Microsoft", "Zebra" },
                result.Items.Select(item => item.Title));
        }





        /// <summary>
        /// Verifies that aggregation sorts content by creation date
        /// in descending order when no sorting options are provided.
        /// </summary>
        [Fact]
        public async Task AggregateAsync_WhenNoSortingSpecified_SortsByDateDescending()
        {
            // Arrange
            var items = new[]
            {
        new ContentItem(
            "1", "Oldest", "https://example.com/1", "George",
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ContentSource.StackExchange, ContentType.Question),

        new ContentItem(
            "2", "Newest", "https://example.com/2", "George",
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            ContentSource.StackExchange, ContentType.Question),

        new ContentItem(
            "3", "Middle", "https://example.com/3", "George",
            new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            ContentSource.StackExchange, ContentType.Question)
    };

            var source = new FakeAggregationSource(
                ContentSource.StackExchange, items);

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var statistics = new RequestStatisticsService();

            var service = new ContentAggregationService(
                new[] { source }, cache, statistics);

            // Act
            var result = await service.AggregateAsync(
                new AggregationQuery(),
                TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                new[] { "Newest", "Middle", "Oldest" },
                result.Items.Select(item => item.Title));
        }







        /// <summary>
        /// Verifies that repeated aggregation requests reuse cached
        /// source results instead of calling the external provider again.
        /// </summary>
        /// <remarks>
        /// Ensures that a successful provider response is cached
        /// and that subsequent requests do not trigger another fetch.
        /// </remarks>
        [Fact]
        public async Task AggregateAsync_WhenCalledTwice_UsesCachedSourceResult()
        {
            // Arrange
            var items = new[]
            {
        new ContentItem(
            "1",
            "Cached Repository",
            "https://example.com/repository",
            "George",
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ContentSource.GitHub,
            ContentType.Repository)
    };

            var source = new FakeAggregationSource(ContentSource.GitHub, items);

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var statistics = new RequestStatisticsService();

            var service = new ContentAggregationService(
                new[] { source }, cache, statistics);

            // Act
            var firstResult = await service.AggregateAsync(
                new AggregationQuery(),
                TestContext.Current.CancellationToken);

            var secondResult = await service.AggregateAsync(
                new AggregationQuery(),
                TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(1, source.CallCount);

            Assert.Single(firstResult.Items);
            Assert.Single(secondResult.Items);

            var githubStatistics = Assert.Single(
                statistics.GetStatistics(),
                item => item.Source == ContentSource.GitHub);

            Assert.Equal(1, githubStatistics.RequestCount);
        }



        /// <summary>
        /// Verifies that each external provider has its own cache entry.
        /// </summary>
        /// <remarks>
        /// Ensures that caching GitHub results does not interfere with
        /// StackExchange results and that each provider is fetched only once.
        /// </remarks>
        [Fact]
        public async Task AggregateAsync_WithMultipleSources_CachesEachSourceIndependently()
        {
            // Arrange
            var githubItem = new ContentItem(
                "1", "GitHub Repository", "https://github.com/example/repo",
                "George",
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                ContentSource.GitHub,
                ContentType.Repository);

            var stackExchangeItem = new ContentItem(
                "2", "StackExchange Question", "https://example.com/question",
                "George",
                new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero),
                ContentSource.StackExchange,
                ContentType.Question);

            var githubSource = new FakeAggregationSource(
                ContentSource.GitHub,
                new[] { githubItem });

            var stackExchangeSource = new FakeAggregationSource(
                ContentSource.StackExchange,
                new[] { stackExchangeItem });

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var statistics = new RequestStatisticsService();

            var service = new ContentAggregationService(
                new IAggregationSource[] { githubSource, stackExchangeSource },
                cache,
                statistics);

            // Act
            var githubResult = await service.AggregateAsync(
                new AggregationQuery { Source = ContentSource.GitHub },
                TestContext.Current.CancellationToken);

            var stackExchangeResult = await service.AggregateAsync(
                new AggregationQuery { Source = ContentSource.StackExchange },
                TestContext.Current.CancellationToken);

            // Repeat both requests to verify cache hits.
            await service.AggregateAsync(
                new AggregationQuery { Source = ContentSource.GitHub },
                TestContext.Current.CancellationToken);

            await service.AggregateAsync(
                new AggregationQuery { Source = ContentSource.StackExchange },
                TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(1, githubSource.CallCount);
            Assert.Equal(1, stackExchangeSource.CallCount);

            Assert.Equal(
                ContentSource.GitHub,
                Assert.Single(githubResult.Items).Source);

            Assert.Equal(
                ContentSource.StackExchange,
                Assert.Single(stackExchangeResult.Items).Source);
        }




        /// <summary>
        /// Verifies that a failure in one external provider does not
        /// prevent successful results from other providers.
        /// </summary>
        /// <remarks>
        /// Simulates an HTTP 500 error from GitHub and verifies that
        /// StackExchange results remain available.
        /// </remarks>
        [Fact]
        public async Task AggregateAsync_WhenOneSourceFails_ReturnsOtherSources()
        {
            // Arrange
            var failingSource = new FailingAggregationSource(ContentSource.GitHub);

            var stackExchangeItem = new ContentItem(
                "1",
                "How does async/await work?",
                "https://example.com/question",
                "George",
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                ContentSource.StackExchange,
                ContentType.Question);

            var successfulSource = new FakeAggregationSource(
                ContentSource.StackExchange,
                new[] { stackExchangeItem });

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var statistics = new RequestStatisticsService();

            var service = new ContentAggregationService(
                new IAggregationSource[] { failingSource, successfulSource },
                cache,
                statistics);

            // Act
            var result = await service.AggregateAsync(
                new AggregationQuery(),
                TestContext.Current.CancellationToken);

            // Assert
            Assert.Single(result.Items);
            Assert.Equal(
                ContentSource.StackExchange,
                result.Items.Single().Source);

            Assert.Equal(2, result.Sources.Count);

            var failedResult = Assert.Single(
                result.Sources,
                source => source.Source == ContentSource.GitHub);

            Assert.False(failedResult.IsSuccessful);
            Assert.Empty(failedResult.Items);
            Assert.NotNull(failedResult.ErrorMessage);

            var successfulResult = Assert.Single(
                result.Sources,
                source => source.Source == ContentSource.StackExchange);

            Assert.True(successfulResult.IsSuccessful);
        }




        // <summary>
        /// Simulates an external provider that fails with HTTP 500.
        /// </summary>
        private sealed class FailingAggregationSource : IAggregationSource
        {
            public ContentSource Source { get; }

            public FailingAggregationSource(ContentSource source)
            {
                Source = source;
            }

            public Task<SourceResult> GetAsync(
                CancellationToken cancellationToken = default)
            {
                throw new HttpRequestException(
                    "Internal Server Error",
                    null,
                    System.Net.HttpStatusCode.InternalServerError);
            }
        }





        /// <summary>
        /// Verifies that failed provider responses are not cached.
        /// </summary>
        /// <remarks>
        /// Ensures that a provider failure does not prevent a successful
        /// retry on the next aggregation request.
        /// </remarks>
        [Fact]
        public async Task AggregateAsync_WhenSourceFails_DoesNotCacheFailure()
        {
            // Arrange
            var source = new RecoveringAggregationSource();

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var statistics = new RequestStatisticsService();

            var service = new ContentAggregationService(
                new IAggregationSource[] { source },
                cache,
                statistics);

            // Act
            var firstResult = await service.AggregateAsync(
                new AggregationQuery(),
                TestContext.Current.CancellationToken);

            var secondResult = await service.AggregateAsync(
                new AggregationQuery(),
                TestContext.Current.CancellationToken);

            // Assert
            Assert.Empty(firstResult.Items);
            Assert.False(Assert.Single(firstResult.Sources).IsSuccessful);

            Assert.Single(secondResult.Items);
            Assert.True(Assert.Single(secondResult.Sources).IsSuccessful);

            Assert.Equal(2, source.CallCount);

            var githubStatistics = Assert.Single(
                statistics.GetStatistics(),
                item => item.Source == ContentSource.GitHub);

            Assert.Equal(2, githubStatistics.RequestCount);
        }





        /// <summary>
        /// Simulates a provider that fails on the first call
        /// and recovers on the second call.
        /// </summary>
        private sealed class RecoveringAggregationSource : IAggregationSource
        {
            public ContentSource Source => ContentSource.GitHub;

            public int CallCount { get; private set; }

            public Task<SourceResult> GetAsync(
                CancellationToken cancellationToken = default)
            {
                CallCount++;

                if (CallCount == 1)
                {
                    throw new HttpRequestException(
                        "Service Unavailable",
                        null,
                        System.Net.HttpStatusCode.ServiceUnavailable);
                }

                var item = new ContentItem(
                    "1",
                    "Recovered Repository",
                    "https://github.com/example/repo",
                    "George",
                    new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    ContentSource.GitHub,
                    ContentType.Repository);

                return Task.FromResult(
                    new SourceResult(Source, new[] { item }, true));
            }
        }











    }
}
