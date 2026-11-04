using AgileActors_Assessment.Application.Interfaces;
using AgileActors_Assessment.Domain.Enums;
using AgileActors_Assessment.Domain.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace AgileActors_Assessment.IntegrationTests
{
    public sealed class AggregationControllerTests
    {
        /// <summary>
        /// Verifies that query parameters are correctly bound
        /// and applied to the aggregated HTTP response.
        /// </summary>
        [Fact]
        public async Task GetAggregation_WithFilters_ReturnsMatchingItems()
        {
            // Arrange
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<IAggregationSource>();

                        services.AddSingleton<IAggregationSource>(
                            new FakeAggregationSource(
                                ContentSource.StackExchange,
                                new[]
                                {
                                new ContentItem(
                                    "1", "Python asyncio question",
                                    "https://example.com/1",
                                    "George",
                                    new DateTimeOffset(
                                        2026, 1, 1, 0, 0, 0,
                                        TimeSpan.Zero),
                                    ContentSource.StackExchange,
                                    ContentType.Question),

                                new ContentItem(
                                    "2", "C# question",
                                    "https://example.com/2",
                                    "George",
                                    new DateTimeOffset(
                                        2026, 1, 2, 0, 0, 0,
                                        TimeSpan.Zero),
                                    ContentSource.StackExchange,
                                    ContentType.Question)
                                }));
                    });
                });

            using var client = factory.CreateClient();

            // Act
            using var response = await client.GetAsync(
                "/api/Aggregation?Query=python&Source=StackExchange&ContentType=Question",
                TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var jsonOptions = new JsonSerializerOptions(
                                 JsonSerializerDefaults.Web);

            jsonOptions.Converters.Add(
                new JsonStringEnumConverter());

            var result = await response.Content.ReadFromJsonAsync<AggregationResult>(
                jsonOptions,
                TestContext.Current.CancellationToken);

            Assert.NotNull(result);

            var item = Assert.Single(result.Items);
            Assert.Equal("Python asyncio question", item.Title);
            Assert.Equal(ContentSource.StackExchange, item.Source);
            Assert.Equal(ContentType.Question, item.ContentType);
        }

        /// <summary>
        /// Returns predefined items without accessing external APIs.
        /// </summary>
        private sealed class FakeAggregationSource : IAggregationSource
        {
            private readonly IReadOnlyCollection<ContentItem> _items;

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
                return Task.FromResult(
                    new SourceResult(Source, _items, true));
            }
        }





        /// <summary>
        /// Verifies that a failure in one external provider does not
        /// prevent the aggregation endpoint from returning successful data.
        /// </summary>
        /// <remarks>
        /// Simulates an HTTP 500 failure from GitHub while StackExchange
        /// succeeds, verifying HTTP 200 and partial failure information.
        /// </remarks>
        [Fact]
        public async Task GetAggregation_WhenOneProviderFails_ReturnsPartialResults()
        {
            // Arrange
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<IAggregationSource>();

                        services.AddSingleton<IAggregationSource>(
                            new FailingAggregationSource(ContentSource.GitHub));

                        var item = new ContentItem(
                            "1",
                            "How does async/await work?",
                            "https://example.com/question",
                            "George",
                            new DateTimeOffset(
                                2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                            ContentSource.StackExchange,
                            ContentType.Question);

                        services.AddSingleton<IAggregationSource>(
                            new FakeAggregationSource(
                                ContentSource.StackExchange,
                                new[] { item }));
                    });
                });

            using var client = factory.CreateClient();

            // Act
            using var response = await client.GetAsync(
                "/api/Aggregation",
                TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());

            var result = await response.Content.ReadFromJsonAsync<AggregationResult>(
                options,
                TestContext.Current.CancellationToken);

            Assert.NotNull(result);

            var itemResult = Assert.Single(result.Items);
            Assert.Equal(ContentSource.StackExchange, itemResult.Source);

            Assert.Equal(2, result.Sources.Count);

            var github = Assert.Single(
                result.Sources,
                source => source.Source == ContentSource.GitHub);

            Assert.False(github.IsSuccessful);
            Assert.Empty(github.Items);
            Assert.NotNull(github.ErrorMessage);

            var stackExchange = Assert.Single(
                result.Sources,
                source => source.Source == ContentSource.StackExchange);

            Assert.True(stackExchange.IsSuccessful);
        }




        /// <summary>
        /// Simulates an external provider returning HTTP 500.
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
                    HttpStatusCode.InternalServerError);
            }
        }


    }
}
