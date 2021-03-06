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
                    if (curTemp != int.MaxValue)
                    {
                        var newCityWeather = new WeatherForecast() { City = city, LastDateTime = DateTime.Now, Temperature = curTemp };
                        _context.Add(newCityWeather);
                        _context.SaveChangesAsync();
                        return newCityWeather;
                    }
                    return null;
                }
                async Task<WeatherForecast> updateFunc(WeatherForecast weather)
                {
                    var curTemp = await GetCurTemp(weather.City);
                    if (curTemp != int.MaxValue)
                    {
                        weather.Temperature = curTemp;
                        _context.SaveChangesAsync();
                    }
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
                var obj = new Object();
                if (dict[0].TryGetValue("Key", out obj))
                {
                    string locationkey = obj as string;
                    var location = "http://dataservice.accuweather.com/forecasts/v1/hourly/12hour/" + locationkey + "?apikey=KL0BN1ASwhUUC4EqCfqnGA0tXsXNEsqA";
                    request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(location),
                    };
                    response = await client.SendAsync(request).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    object contentObj = new object(); ;
                    var dict1 = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(responseBody);
                    if (dict1[0].TryGetValue("Temperature", out contentObj))
                    {
                        var content = JsonConvert.SerializeObject(contentObj);
                        string[] items = content.TrimStart('{').TrimEnd('}').Split(',');
                        foreach (var item in items)
                        {
                            var index = item.Split(':');
                            if (index[0].Trim('\"') == "Value")
                                return (int)Convert.ToDouble(index[1]);
                        }
                    }
                }
            }
            catch
            {
               throw;
            }
            return int.MaxValue;
        }
    }

    public interface IWeatherService
    {
        Task<WeatherForecast> GetWeather(string city);
    }
}
