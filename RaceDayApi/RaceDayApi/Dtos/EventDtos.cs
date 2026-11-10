using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RaceDayApi.Models;

namespace RaceDayApi.Dtos;

/// <summary>Data for creating or updating an event.</summary>
public class EventRequest
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly? EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    /// <summary>The event description / route description.</summary>
    [Required, StringLength(500)]
    public string RouteDescription { get; set; } = string.Empty;

    /// <summary>"Run", "Walk" or "Cycle".</summary>
    [Required]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Optional on create only: adds a first category (e.g. "10km") with this distance.</summary>
    [Range(0.1, 1000)]
    public decimal? DistanceKm { get; set; }
}

public class CategoryRequest
{
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, Range(0.1, 1000)]
    public decimal? DistanceKm { get; set; }
}

public class CategoryResponse
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int EnrolmentCount { get; set; }

    public static readonly Expression<Func<Category, CategoryResponse>> Projection = c => new CategoryResponse
    {
        CategoryID = c.CategoryID,
        EventID = c.EventID,
        Name = c.Name,
        DistanceKm = c.DistanceKm,
        EnrolmentCount = c.Enrolments.Count(x => x.Status != "Cancelled")
    };
}

public class EventResponse
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? RouteDescription { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? BannerUrl { get; set; }
    public int EnrolmentCount { get; set; }
    public List<CategoryResponse> Categories { get; set; } = new();

    public static readonly Expression<Func<Event, EventResponse>> Projection = e => new EventResponse
    {
        EventID = e.EventID,
        OrganiserID = e.OrganiserID,
        OrganiserName = e.Organiser.Name,
        Name = e.Name,
        EventDate = e.EventDate,
        Location = e.Location,
        RouteDescription = e.RouteDescription,
        EventType = e.EventType,
        BannerUrl = e.BannerUrl,
        EnrolmentCount = e.Categories.SelectMany(c => c.Enrolments).Count(x => x.Status != "Cancelled"),
        Categories = e.Categories.OrderBy(c => c.DistanceKm).Select(c => new CategoryResponse
        {
            CategoryID = c.CategoryID,
            EventID = c.EventID,
            Name = c.Name,
            DistanceKm = c.DistanceKm,
            EnrolmentCount = c.Enrolments.Count(x => x.Status != "Cancelled")
        }).ToList()
    };
}
