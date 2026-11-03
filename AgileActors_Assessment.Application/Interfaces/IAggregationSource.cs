using AgileActors_Assessment.Domain.Enums;
using AgileActors_Assessment.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgileActors_Assessment.Application.Interfaces
{
    public interface IAggregationSource
    {
        ContentSource Source { get; }

        Task<SourceResult> GetAsync(
            CancellationToken cancellationToken = default);
    }
}
