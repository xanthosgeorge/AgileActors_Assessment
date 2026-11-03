using AgileActors_Assessment.Domain.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgileActors_Assessment.Application.Statistcs
{
    public sealed class RequestStatisticsService
    {
        private readonly ConcurrentDictionary<ContentSource, SourceCounters>
            _counters = new();

        public void Record(ContentSource source, long elapsedMilliseconds)
        {
            var counters = _counters.GetOrAdd(
                source, _ => new SourceCounters());

            Interlocked.Increment(ref counters.RequestCount);
            Interlocked.Add(ref counters.TotalElapsedMilliseconds,
                elapsedMilliseconds);

            if (elapsedMilliseconds < 100)
            {
                Interlocked.Increment(ref counters.Under100Ms);
            }
            else if (elapsedMilliseconds <= 500)
            {
                Interlocked.Increment(ref counters.Between100And500Ms);
            }
            else
            {
                Interlocked.Increment(ref counters.Over500Ms);
            }
        }

        public IReadOnlyCollection<SourceStatistics> GetStatistics()
        {
            return Enum.GetValues<ContentSource>()
                .Select(source =>
                {
                    _counters.TryGetValue(source, out var counters);

                    var count = counters is null
                        ? 0
                        : Interlocked.Read(ref counters.RequestCount);

                    var total = counters is null
                        ? 0
                        : Interlocked.Read(ref counters.TotalElapsedMilliseconds);

                    return new SourceStatistics(
                        source,
                        count,
                        count == 0 ? 0 : (double)total / count,
                        counters is null ? 0 : Interlocked.Read(ref counters.Under100Ms),
                        counters is null ? 0 : Interlocked.Read(ref counters.Between100And500Ms),
                        counters is null ? 0 : Interlocked.Read(ref counters.Over500Ms));
                })
                .ToArray();
        }

        private sealed class SourceCounters
        {
            public long RequestCount;
            public long TotalElapsedMilliseconds;
            public long Under100Ms;
            public long Between100And500Ms;
            public long Over500Ms;
        }
    }

    public sealed record SourceStatistics(
        ContentSource Source,
        long RequestCount,
        double AverageResponseTimeMs,
        long Under100Ms,
        long Between100And500Ms,
        long Over500Ms);
}
