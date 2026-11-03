using AgileActors_Assessment.Application.Interfaces;
using AgileActors_Assessment.Infrastructure.ExternalApis;
using Microsoft.Extensions.DependencyInjection;


namespace AgileActors_Assessment.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services)
        {
            services.AddHttpClient<IAggregationSource, GitHubSource>("GitHub",client =>
            {
                client.BaseAddress = new Uri("https://api.github.com/");

                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "AgileActorsAssessment/1.0");

                client.DefaultRequestHeaders.Accept.ParseAdd(
                    "application/vnd.github+json");

                client.DefaultRequestHeaders.Add(
                    "X-GitHub-Api-Version",
                    "2026-03-10");
            });


            services.AddHttpClient<IAggregationSource, HackerNewsSource>("HackerNews",client =>
            {
                client.BaseAddress = new Uri(
                    "https://hacker-news.firebaseio.com/v0/");
            });



            services.AddHttpClient<IAggregationSource, StackExchangeSource>(client =>
            {
                client.BaseAddress = new Uri(
                    "https://api.stackexchange.com/2.3/");

                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "AgileActorsAssessment/1.0");
            });

            return services;
        }
    }
}
