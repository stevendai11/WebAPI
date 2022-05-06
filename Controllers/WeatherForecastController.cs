using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebAPI.Models;
using WebAPI.Services;
namespace WebAPI.Controllers
{
    [ApiController]
     public class WeatherForecastController : ControllerBase
    {
        private readonly IWeatherService _weatherService;

        public WeatherForecastController(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }
         
        [HttpGet]
        [Route("WeatherForecastController/{city?}")]
        public async Task<ActionResult<WeatherForecast>> Get([FromQuery] string city)
        {
            try
            {
                return await _weatherService.GetWeather(city);
            }
            catch (Exception ex)
            {
                //to do, return more meaningful error message
                return  NotFound();
            }
        }
    }
}
