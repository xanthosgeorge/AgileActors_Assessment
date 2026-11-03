using AgileActors_Assessment.Application.Interfaces;
using AgileActors_Assessment.Domain.Enums;
using AgileActors_Assessment.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace AgileActors_Assessment.Infrastructure.ExternalApis
{
    public sealed class StackExchangeSource(HttpClient httpClient) : IAggregationSource
    {
        public ContentSource Source => ContentSource.StackExchange;

        public async Task<SourceResult> GetAsync(
            CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetFromJsonAsync<StackExchangeResponse>(
                "questions?site=stackoverflow&order=desc&sort=creation&pagesize=10",
                cancellationToken);

            if (response?.Items is null)
            {
                return new SourceResult(
                    Source,
                    [],
                    false,
                    "Stack Exchange returned no questions.");
            }

            var items = response.Items
                .Select(item => new ContentItem(
                    item.QuestionId.ToString(),
                    item.Title,
                    item.Link,
                    item.Owner?.DisplayName,
                    DateTimeOffset.FromUnixTimeSeconds(item.CreationDate),
                    Source,
                    ContentType.Question))
                .ToArray();

            return new SourceResult(Source, items, true);
        }

        private sealed class StackExchangeResponse
        {
            public List<StackExchangeQuestion>? Items { get; init; }
        }

        private sealed class StackExchangeQuestion
        {
            public long QuestionId { get; init; }

            public string Title { get; init; } = string.Empty;

            public string Link { get; init; } = string.Empty;

            public long CreationDate { get; init; }

            public StackExchangeOwner? Owner { get; init; }
        }

        private sealed class StackExchangeOwner
        {
            public string? DisplayName { get; init; }
        }
    }
}
