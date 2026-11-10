using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RaceDayApi.Models;

namespace RaceDayApi.Dtos;

public class EnrolmentResponse
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }

    public static readonly Expression<Func<Enrolment, EnrolmentResponse>> Projection = en => new EnrolmentResponse
    {
        EnrolmentID = en.EnrolmentID,
        ParticipantID = en.ParticipantID,
        ParticipantName = en.Participant.Name,
        ParticipantEmail = en.Participant.Email,
        CategoryID = en.CategoryID,
        CategoryName = en.Category.Name,
        DistanceKm = en.Category.DistanceKm,
        EventID = en.Category.EventID,
        EventName = en.Category.Event.Name,
        EventDate = en.Category.Event.EventDate,
        Location = en.Category.Event.Location,
        EnrolmentDate = en.EnrolmentDate,
        Status = en.Status,
        FinishTime = en.Result != null ? en.Result.FinishTime : null,
        Position = en.Result != null ? en.Result.Position : null
    };
}

public class UpdateEnrolmentStatusRequest
{
    /// <summary>"Pending", "Confirmed" or "Cancelled".</summary>
    [Required]
    public string Status { get; set; } = string.Empty;
}

public class ResultRequest
{
    [Required]
    public TimeSpan? FinishTime { get; set; }

    [Required, Range(1, 1_000_000)]
    public int? Position { get; set; }
}

public class ResultResponse
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }

    /// <summary>Number of finishers in the same category (for "47th out of 312").</summary>
    public int TotalFinishers { get; set; }
}
