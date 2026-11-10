using Microsoft.Extensions.Logging;

namespace RaceDayApi.Models;

public class WeatherInfo
{
    public int WeatherID { get; set; }
    public int EventID { get; set; }
    public string? ForecastSummary { get; set; }
    public decimal? TemperatureC { get; set; }

    public Event Event { get; set; } = null!;
}