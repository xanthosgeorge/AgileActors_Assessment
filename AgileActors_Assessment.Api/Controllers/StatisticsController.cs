using AgileActors_Assessment.Application.Statistcs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgileActors_Assessment.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly RequestStatisticsService _statistics;

        public StatisticsController(RequestStatisticsService statistics)
        {
            _statistics = statistics;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                Sources = _statistics.GetStatistics()
            });
        }
    }
}
