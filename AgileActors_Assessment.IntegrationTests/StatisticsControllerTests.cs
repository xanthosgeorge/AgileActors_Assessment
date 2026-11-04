using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AgileActors_Assessment.IntegrationTests
{
    public sealed class StatisticsControllerTests
    {
        /// <summary>
        /// Verifies that the statistics endpoint responds successfully
        /// through the ASP.NET Core HTTP pipeline.
        /// </summary>
        [Fact]
        public async Task GetStatistics_ReturnsHttp200()
        {
            // Arrange
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            // Act
            using var response = await client.GetAsync(
                "/api/statistics",
                TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
