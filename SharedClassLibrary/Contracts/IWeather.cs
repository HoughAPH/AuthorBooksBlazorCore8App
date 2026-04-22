using SharedClassLibrary.Models;

namespace SharedClassLibrary.Contracts
{
    public interface IWeather
    {
        Task<WeatherForecast[]> GetWeatherForecast();
    }
}
