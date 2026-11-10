using System.ComponentModel.DataAnnotations;

namespace RaceDayWeb.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your name."), StringLength(100)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "Please enter a valid email address."), StringLength(150)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please choose a password."), DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters long.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password."), DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Please choose how you will use RaceDay.")]
    public string Role { get; set; } = "Participant";
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your name."), StringLength(100)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "Please enter a valid email address."), StringLength(150)]
    public string Email { get; set; } = "";

    public string Role { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }
}

public class EventFormModel
{
    public int? EventID { get; set; }

    [Required(ErrorMessage = "Please enter the event name."), StringLength(150)]
    [Display(Name = "Event name")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Please enter a description."), StringLength(500, ErrorMessage = "The description can be at most 500 characters.")]
    [Display(Name = "Description / route")]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Date")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location."), StringLength(150)]
    public string Location { get; set; } = "";

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Main distance (km)")]
    public decimal? DistanceKm { get; set; }

    public string? BannerUrl { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }
}

public class CategoryFormModel
{
    public int? CategoryID { get; set; }
    public int EventID { get; set; }

    [Required(ErrorMessage = "Please enter a category name (e.g. 10km Run)."), StringLength(50)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Please enter the distance.")]
    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }
}

public class ResultFormModel
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Enter the finish time.")]
    [RegularExpression(@"^([01]?\d|2[0-3]):[0-5]\d:[0-5]\d$", ErrorMessage = "Use the format hh:mm:ss, e.g. 00:52:30.")]
    public string FinishTime { get; set; } = "";

    [Required(ErrorMessage = "Enter the position.")]
    [Range(1, 1000000, ErrorMessage = "Position must be 1 or higher.")]
    public int? Position { get; set; }
}

public class EventFilterModel
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class EventListViewModel
{
    public EventFilterModel Filter { get; set; } = new();
    public List<EventDto> Events { get; set; } = new();
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();

    /// <summary>The signed-in participant's active enrolment in this event, if any.</summary>
    public EnrolmentDto? MyEnrolment { get; set; }
}

public class DashboardViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public List<EventDto> Upcoming => Events.Where(e => !e.IsPast).OrderBy(e => e.EventDate).ToList();
}

public class EventEnrolmentsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ErrorViewModel
{
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "We couldn't complete that request. Please try again.";
}
