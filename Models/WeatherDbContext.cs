using Microsoft.EntityFrameworkCore;
using System.Data.Entity;
using WebAPI.Models;
namespace WebAPI.Models
{
    public class WeatherDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options)
        {
        }
        public Microsoft.EntityFrameworkCore.DbSet<WeatherForecast> WeatherForecastList { get; set; }
    }
}
