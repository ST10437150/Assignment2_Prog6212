namespace RaceDayApi.Models;

public class Event
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? RouteDescription { get; set; }
    public string EventType { get; set; } = "Run"; // "Run", "Walk" or "Cycle"

    public User Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<WeatherInfo> WeatherInfos { get; set; } = new List<WeatherInfo>();
}