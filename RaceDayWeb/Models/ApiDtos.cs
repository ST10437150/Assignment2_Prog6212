namespace RaceDayWeb.Models;

// Plain copies of the JSON shapes returned by the RaceDay API.

public class UserDto
{
    public int UserID { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }
}

public class LoginResultDto
{
    public string Token { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public UserDto User { get; set; } = new();
}

public class CategoryDto
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int EnrolmentCount { get; set; }
}

public class EventDto
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string OrganiserName { get; set; } = "";
    public string Name { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = "";
    public string? RouteDescription { get; set; }
    public string EventType { get; set; } = "";
    public string? BannerUrl { get; set; }
    public int EnrolmentCount { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();

    public bool IsPast => EventDate < DateOnly.FromDateTime(DateTime.UtcNow);
}

public class EnrolmentDto
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = "";
    public string ParticipantEmail { get; set; } = "";
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = "";
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = "";
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
}

public class ResultDto
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public string ParticipantName { get; set; } = "";
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public int TotalFinishers { get; set; }
}
