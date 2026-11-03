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
        public string? Query { get; set; }

        public ContentSource? Source { get; set; }

        public ContentType? ContentType { get; set; }

        public DateTimeOffset? From { get; set; }

        public DateTimeOffset? To { get; set; }

        public string SortBy { get; set; } = "date";

        public string SortDirection { get; set; } = "desc";
    }
}
