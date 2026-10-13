namespace RaceDay.Api.Models;

public class Event
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EventType { get; set; } = EventTypes.Running;
    public string Status { get; set; } = EventStatuses.Scheduled;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Organiser Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
