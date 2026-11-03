using AgileActors_Assessment.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgileActors_Assessment.Domain.Models
{
    public sealed record ContentItem(
    string Id,
    string Title,
    string Url,
    string? Author,
    DateTimeOffset CreatedAt,
    ContentSource Source,   
    ContentType ContentType);
}
