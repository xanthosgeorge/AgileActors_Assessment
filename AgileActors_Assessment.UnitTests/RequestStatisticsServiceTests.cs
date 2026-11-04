using AgileActors_Assessment.Application.Statistics;
using AgileActors_Assessment.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AgileActors_Assessment.UnitTests
{


    public sealed class RequestStatisticsServiceTests
    {
        /// <summary>
        /// Verifies that concurrent statistics updates do not lose
        /// requests and that performance buckets are calculated correctly.
        /// </summary>
        [Fact]
        public async Task Record_WhenCalledConcurrently_TracksAllRequests()
        {
            // Arrange
            var statistics = new RequestStatisticsService();

            const int requestCount = 1000;

            // Act
            await Task.WhenAll(
                Enumerable.Range(0, requestCount)
                    .Select(i => Task.Run(() =>
                    {
                        var duration = (i % 3) switch
                        {
                            0 => 50L,
                            1 => 250L,
                            _ => 750L
                        };

                        statistics.Record(ContentSource.GitHub, duration);
                    }, TestContext.Current.CancellationToken)));

            // Assert
            var github = Assert.Single(
                statistics.GetStatistics(),
                item => item.Source == ContentSource.GitHub);

            Assert.Equal(requestCount, github.RequestCount);

            Assert.Equal(334, github.Under100Ms);
            Assert.Equal(333, github.Between100And500Ms);
            Assert.Equal(333, github.Over500Ms);

            Assert.Equal(
                (334 * 50d + 333 * 250d + 333 * 750d) / requestCount,
                github.AverageResponseTimeMs,
                precision: 2);
        }



        /// <summary>
        /// Verifies that response times at performance bucket
        /// boundaries are categorized correctly.
        /// </summary>
        [Fact]
        public void Record_WhenDurationIsAtBucketBoundary_UsesCorrectBucket()
        {
            // Arrange
            var statistics = new RequestStatisticsService();

            // Act
            statistics.Record(ContentSource.GitHub, 99);
            statistics.Record(ContentSource.GitHub, 100);
            statistics.Record(ContentSource.GitHub, 500);
            statistics.Record(ContentSource.GitHub, 501);

            // Assert
            var github = Assert.Single(
                statistics.GetStatistics(),
                item => item.Source == ContentSource.GitHub);

            Assert.Equal(4, github.RequestCount);
            Assert.Equal(1, github.Under100Ms);
            Assert.Equal(2, github.Between100And500Ms);
            Assert.Equal(1, github.Over500Ms);

            Assert.Equal(300, github.AverageResponseTimeMs);
        }


    }
}
