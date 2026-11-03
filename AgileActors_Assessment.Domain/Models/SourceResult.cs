using AgileActors_Assessment.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgileActors_Assessment.Domain.Models
{
    public sealed record SourceResult(
     ContentSource Source,
     IReadOnlyCollection<ContentItem> Items,
     bool IsSuccessful,
     string? ErrorMessage = null);
}
