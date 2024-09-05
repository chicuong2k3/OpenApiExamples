using Microsoft.AspNetCore.Mvc;

namespace OpenApiExamples.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get a book by id
        /// </summary>
        /// <param name="id">The id of the book you want to get</param>
        /// <returns>A book with the specified id</returns>
        /// <remarks>
        /// **Sample request**:  
        ///     GET /WeatherForecast/12345678-1234-1234-1234-123456789012  
        ///     
        ///     {
        ///         
        ///     }
        /// </remarks>
        [HttpGet]
        public IActionResult Get(Guid id)
        {
            return Ok();
        }
    }
}
