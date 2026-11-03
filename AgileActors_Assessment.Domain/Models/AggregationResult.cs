using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgileActors_Assessment.Domain.Models
{
    public sealed record AggregationResult(
     IReadOnlyCollection<ContentItem> Items,
     IReadOnlyCollection<SourceResult> Sources);
}
