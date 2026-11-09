namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; } = 100;

    /// <summary>"Distance" (a route length such as 10km) or "Age" (an age group such as 12-17).</summary>
    public string CategoryType { get; set; } = CategoryTypes.Distance;
    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }

    public Event Event { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
