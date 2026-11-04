# API Aggregation Service

A RESTful API built with **ASP.NET Core (.NET 8)** that retrieves content from multiple external providers concurrently, normalizes the results into a common data model, and supports filtering, sorting, caching, and request statistics.

## Features

- **Multi-source aggregation:** Integrates GitHub, Hacker News, and Stack Exchange.
- **Concurrent execution:** Uses `Task.WhenAll` to retrieve data from multiple providers concurrently.
- **Unified data model:** Converts different API responses into a consistent `ContentItem` representation.
- **Filtering:** Supports keyword, source, content type, and creation date filters.
- **Sorting:** Supports ascending and descending ordering by title or creation date.
- **Fault tolerance:** Handles supported provider failures independently, allowing partial results.
- **In-memory caching:** Caches successful provider results for 60 seconds.
- **Request statistics:** Tracks provider fetch counts, average execution times, and performance buckets using thread-safe counters.
- **Automated testing:** Includes unit and HTTP integration tests.

## Tech Stack

- .NET 8 / ASP.NET Core Web API
- C#
- `HttpClientFactory`
- `IMemoryCache`
- `ConcurrentDictionary` and `Interlocked`
- xUnit v3
- `WebApplicationFactory` for integration testing
- Swagger / OpenAPI

## Architecture

The solution uses a lightweight Clean Architecture approach with separate responsibilities across projects.

```text
AgileActors_Assessment/
├── AgileActors_Assessment.Api/
│   ├── Controllers/
│   └── Program.cs
├── AgileActors_Assessment.Application/
│   ├── Interfaces/
│   ├── Models/
│   ├── Services/
│   └── Statistics/
├── AgileActors_Assessment.Domain/
│   ├── Enums/
│   └── Models/
├── AgileActors_Assessment.Infrastructure/
│   └── ExternalApis/
├── AgileActors_Assessment.UnitTests/
└── AgileActors_Assessment.IntegrationTests/
```

**Project responsibilities:**

- **Domain:** Common content models, source results, and enumerations.
- **Application:** Aggregation orchestration, filtering, sorting, caching coordination, fault handling, and statistics.
- **Infrastructure:** External API integrations using typed HTTP clients.
- **API:** HTTP endpoints, request binding, dependency injection, and JSON configuration.
- **Tests:** Isolated business-logic tests and HTTP pipeline integration tests.

External providers implement the `IAggregationSource` interface, making integrations replaceable and testable without real HTTP requests.

## External Data Sources

| Provider | Content | Retrieval |
|---|---|---|
| GitHub | Repositories | Recent results from a predefined .NET-related repository search |
| Hacker News | Articles/stories | Top story IDs followed by individual story requests |
| Stack Exchange | Questions | Recent Stack Overflow questions |

Provider responses are normalized into the following content fields:

`Id`, `Title`, `Url`, `Author`, `CreatedAt`, `Source`, `ContentType`.

## Getting Started

### Prerequisites

- .NET 8 SDK
- Internet connectivity for external API requests
- .NET 10 SDK for running tests via the configured Microsoft Testing Platform runner.

No database or API credentials are required for the current public API integrations.

### Run the application

Clone the repository and navigate to its root directory.

```bash
dotnet restore
dotnet build
dotnet run --project AgileActors_Assessment.Api
```

Use the local URL printed by the application to access the API.

When enabled in the development environment, Swagger UI is available at `/swagger`.

### Run the tests

From the solution root:

```bash
dotnet test
```

The solution includes unit tests for aggregation, filtering, sorting, caching, failure handling, and statistics, as well as integration tests for the HTTP endpoints.

## API Endpoints

### GET /api/Aggregation

Retrieves, combines, filters, and sorts content from the configured external providers.

**Supported query parameters:**

| Parameter | Description | Default |
|---|---|---|
| `query` | Case-insensitive keyword matching against content titles | No filter |
| `source` | `GitHub`, `HackerNews`, or `StackExchange` | All sources |
| `contentType` | `Repository`, `Article`, or `Question` | All types |
| `from` | Minimum creation timestamp (inclusive) | No limit |
| `to` | Maximum creation timestamp (inclusive) | No limit |
| `sortBy` | `date` or `title` | `date` |
| `sortDirection` | `asc` or `desc` | `desc` |

Filters are combined using **AND** conditions.

Example:

```http
GET /api/Aggregation?query=python&source=StackExchange&contentType=Question&sortBy=date&sortDirection=desc
```

The response includes:

- `items`: The final filtered and sorted content collection.
- `sources`: Provider-level results, including fetched items, success status, and error messages.

The `sources` collection contains the original provider results before global filtering. Consumers should use `items` for the final aggregated list.

### GET /api/statistics

Returns runtime statistics grouped by external provider.

Example response:

```json
{
  "sources": [
    {
      "source": "GitHub",
      "requestCount": 3,
      "averageResponseTimeMs": 245.67,
      "under100Ms": 1,
      "between100And500Ms": 2,
      "over500Ms": 0
    }
  ]
}
```

The statistics track provider fetch attempts, not inbound requests to the aggregation endpoint.

A Hacker News fetch that retrieves multiple individual stories is counted as one provider fetch operation.

Performance buckets are defined as:

- `under100Ms`: Response time below 100 ms.
- `between100And500Ms`: Response time from 100 through 500 ms.
- `over500Ms`: Response time above 500 ms.

Statistics are maintained in memory and reset when the application restarts.

## Concurrency and Fault Tolerance

External providers are fetched concurrently using `Task.WhenAll`.

Failures are handled separately for each provider. Supported HTTP, timeout, and JSON parsing failures are represented as unsuccessful source results instead of causing the entire aggregation operation to fail.

Successful results from other providers remain available.

Request statistics use a `ConcurrentDictionary` and atomic operations through `Interlocked` to support concurrent updates.

## Caching

Successful external provider results are stored using `IMemoryCache`.

- **Cache key:** Unique per provider.
- **Expiration:** 60 seconds, using absolute expiration.
- **Cache hits:** Avoid additional provider fetches.
- **Failed requests:** Not cached.
- **Filtering and sorting:** Applied after retrieving cached or fresh provider results.

Caching is performed at the provider-result level, allowing different filtering and sorting requests to reuse the same cached content.

## Testing

The solution includes **15 automated tests**:

| Area | Tests |
|---|---:|
| Aggregation filtering and sorting | 6 |
| Caching and fault tolerance | 4 |
| Thread-safe statistics | 2 |
| HTTP integration tests | 3 |
| **Total** | **15** |

Integration tests use `WebApplicationFactory` with fake provider implementations, verifying routing, query binding, JSON responses, and partial failure behavior without relying on external services.

## Design Decisions and Known Limitations

The implementation intentionally prioritizes clear responsibilities, testability, and simplicity.

- **Limited provider results:** Each integration retrieves a predefined set of content rather than the provider's complete dataset.
- **In-memory filtering:** The keyword filter is applied to retrieved content titles; it is not a full-text search across external platforms.
- **In-process cache:** Cached data is local to the application instance and is cleared on restart.
- **Concurrent cache misses:** Multiple simultaneous requests for an uncached provider can trigger duplicate fetches; single-flight request coordination is not implemented.
- **In-memory statistics:** Metrics are not persisted or shared across application instances. Concurrent reads may observe updates occurring between individual counter reads.
- **Provider fetch metrics:** Statistics measure the complete provider retrieval operation rather than each individual downstream HTTP call.
- **No authentication:** The endpoints are currently unauthenticated.

Potential future enhancements include distributed caching, persistent observability, request coalescing, pagination, and provider-specific searching.

## Summary

This project demonstrates API integration, concurrent asynchronous programming, separation of concerns, caching, fault tolerance, automated testing, and thread-safe runtime metrics using modern .NET practices.