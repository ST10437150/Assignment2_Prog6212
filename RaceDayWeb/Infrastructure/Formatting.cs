namespace RaceDayWeb.Infrastructure;

/// <summary>Small display helpers used by the views.</summary>
public static class Formatting
{
    public static string Ordinal(int n)
    {
        var lastTwo = n % 100;
        if (lastTwo is >= 11 and <= 13) return $"{n}th";
        return (n % 10) switch { 1 => $"{n}st", 2 => $"{n}nd", 3 => $"{n}rd", _ => $"{n}th" };
    }

    public static string Time(TimeSpan? time) =>
        time.HasValue ? time.Value.ToString(@"hh\:mm\:ss") : "-";

    public static string Km(decimal km) => km.ToString("0.##") + " km";

    /// <summary>CSS class that colour-codes an enrolment status.</summary>
    public static string StatusClass(string status) => status switch
    {
        "Confirmed" => "status-confirmed",
        "Pending" => "status-pending",
        _ => "status-cancelled"
    };
}
