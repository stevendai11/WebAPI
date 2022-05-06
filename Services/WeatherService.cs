using Microsoft.EntityFrameworkCore;
using WebAPI.Models;
using WebAPI.Services;
using System.Collections;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace WebAPI.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly WeatherDbContext _context;
        const int ExpiringHours = 4;
        public WeatherService(WeatherDbContext ctx)
        {
            _context = ctx;
        }

        private SemaphoreSlim mySemaphoreSlim = new SemaphoreSlim(1, 1);
        public async Task<WeatherForecast> GetWeather(string city)
        {
            await mySemaphoreSlim.WaitAsync();
            try
            { 
                var weather = _context.WeatherForecastList.Count() > 0 ? (from w in _context.WeatherForecastList where w.City == city select w).FirstOrDefault() : null;
                var curDateTime = DateTime.Now;

                if (weather != null)
                {
                    if (curDateTime.Subtract(weather.LastDateTime).TotalHours < ExpiringHours)
                        return weather;
                    else
                        return await updateFunc(weather);
                }
                else
                {
                    var curTemp = await GetCurTemp(city);
                    var newCityWeather = new WeatherForecast() { City = city, LastDateTime = DateTime.Now, Temperature = curTemp };
                    _context.Add(newCityWeather);
                    _context.SaveChangesAsync();
                    return newCityWeather;
                }
                async Task<WeatherForecast> updateFunc(WeatherForecast weather)
                {
                    var curTemp = await GetCurTemp(weather.City);
                    weather.Temperature = curTemp;
                    _context.SaveChangesAsync();
                    return weather;
                };
            }
            catch (Exception ex)
            {
                throw;
            }
            finally {
                mySemaphoreSlim.Release();
            }
}
        private async Task<int> GetCurTemp(string city)
        {
            try
            {
                var citySearch = "http://dataservice.accuweather.com/locations/v1/cities/search?apikey=KL0BN1ASwhUUC4EqCfqnGA0tXsXNEsqA&q="+city;
                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(citySearch),
                };
                var response = await client.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var dict = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(responseBody);
                foreach (var key in dict[0].Keys)
                {
                    if (key == "Key")
                    {
                        string locationkey = dict[0][key] as string;
                        var location = "http://dataservice.accuweather.com/forecasts/v1/hourly/12hour/" + locationkey + "?apikey=KL0BN1ASwhUUC4EqCfqnGA0tXsXNEsqA";
                        request = new HttpRequestMessage
                        {
                            Method = HttpMethod.Get,
                            RequestUri = new Uri(location),
                        };
                        response = await client.SendAsync(request).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();
                        responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        dict = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(responseBody);
                        foreach (var key1 in dict[0].Keys)
                        {
                            if (key1 == "Temperature")
                            {
                                var values = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(dict[0][key1] as string);
                                foreach (var d in values)
                                {
                                    foreach (var k1 in d.Keys)
                                        if (k1 == "Temperature")
                                        {
                                            return Convert.ToInt32(d[k1]);
                                        }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
               throw;
            }
            return 0;
        }
    }

    public interface IWeatherService
    {
        Task<WeatherForecast> GetWeather(string city);
    }
}
