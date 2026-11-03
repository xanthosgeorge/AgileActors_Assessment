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
    public sealed class HackerNewsSource(HttpClient httpClient) : IAggregationSource
    {
        public ContentSource Source => ContentSource.HackerNews;

        public async Task<SourceResult> GetAsync(
            CancellationToken cancellationToken = default)
        {
            var topStoryIds = await httpClient.GetFromJsonAsync<List<long>>(
                "topstories.json",
                cancellationToken);

            if (topStoryIds is null || topStoryIds.Count == 0)
            {
                return new SourceResult(
                    Source,
                    [],
                    false,
                    "Hacker News returned no stories.");
            }

            var selectedIds = topStoryIds.Take(10);

            var storyTasks = selectedIds.Select(id =>
                httpClient.GetFromJsonAsync<HackerNewsStory>(
                    $"item/{id}.json",
                    cancellationToken));

            var stories = await Task.WhenAll(storyTasks);

            var items = stories
                .Where(story => story is not null)
                .Select(story => new ContentItem(
                    story!.Id.ToString(),
                    story.Title ?? string.Empty,
                    story.Url ?? $"https://news.ycombinator.com/item?id={story.Id}",
                    story.By,
                    DateTimeOffset.FromUnixTimeSeconds(story.Time),
                    Source,
                    ContentType.Article))
                .ToArray();

            return new SourceResult(Source, items, true);
        }

        private sealed class HackerNewsStory
        {
            public long Id { get; init; }

            public string? Title { get; init; }

            public string? Url { get; init; }

            public string? By { get; init; }

            public long Time { get; init; }
        }
    }
}
