using AgileActors_Assessment.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgileActors_Assessment.Application.Models
{
    public sealed class AggregationQuery
    {
        public string? Query { get; init; }

        public ContentSource? Source { get; init; }

        public ContentType? ContentType { get; init; }

        public DateTimeOffset? From { get; init; }

        public DateTimeOffset? To { get; init; }

        public string SortBy { get; init; } = "date";

        public string SortDirection { get; init; } = "desc";
    }
}
