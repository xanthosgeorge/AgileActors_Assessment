using AgileActors_Assessment.Application.Interfaces;
using AgileActors_Assessment.Domain.Enums;
using AgileActors_Assessment.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AgileActors_Assessment.Infrastructure.ExternalApis
{
    public sealed class GitHubSource(HttpClient httpClient) : IAggregationSource
    {
        public ContentSource Source => ContentSource.GitHub;

    
        public async Task<SourceResult> GetAsync(
    CancellationToken cancellationToken = default)
        {
            const string requestUrl =
                "search/repositories?q=topic%3Adotnet&sort=updated&order=desc&per_page=10";

            var httpResponse = await httpClient.GetAsync(
                requestUrl,
                cancellationToken);

            var body = await httpResponse.Content.ReadAsStringAsync(
                cancellationToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"GitHub returned {(int)httpResponse.StatusCode} " +
                    $"{httpResponse.StatusCode}: {body}");
            }

            var response = System.Text.Json.JsonSerializer.Deserialize<GitHubResponse>(
                body,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (response?.Items is null)
            {
                return new SourceResult(
                    Source,
                    [],
                    false,
                    "GitHub returned no data.");
            }

            var items = response.Items
                .Select(item => new ContentItem(
                    item.Id.ToString(),
                    item.FullName,
                    item.HtmlUrl,
                    item.Owner?.Login,
                    item.CreatedAt,
                    Source,
                    ContentType.Repository))
                .ToArray();

            return new SourceResult(Source, items, true);
        }



        private sealed class GitHubResponse
        {
            public List<GitHubRepository>? Items { get; init; }
        }

        private sealed class GitHubRepository
        {
            public long Id { get; init; }

            [JsonPropertyName("full_name")]
            public string FullName { get; init; } = string.Empty;

            [JsonPropertyName("html_url")]
            public string HtmlUrl { get; init; } = string.Empty;
            public GitHubOwner? Owner { get; init; }

            [JsonPropertyName("created_at")]
            public DateTimeOffset CreatedAt { get; init; }
        }

        private sealed class GitHubOwner
        {
            public string Login { get; init; } = string.Empty;
        }
    }
}
