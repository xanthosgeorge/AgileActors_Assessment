using AgileActors_Assessment.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgileActors_Assessment.Domain.Models
{
    /// <summary>
    /// Represents the outcome of fetching content from an external provider.
    /// </summary>
    /// <param name="IsFallback">
    /// Indicates that the returned items originate from previously
    /// cached successful data after a provider failure.
    /// </param>
    public sealed record SourceResult(
        ContentSource Source,
        IReadOnlyCollection<ContentItem> Items,
        bool IsSuccessful,
        string? ErrorMessage = null,
        bool IsFallback = false);
}
