namespace RaceDayApi.Models;

public class Category
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }

    public Event Event { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}