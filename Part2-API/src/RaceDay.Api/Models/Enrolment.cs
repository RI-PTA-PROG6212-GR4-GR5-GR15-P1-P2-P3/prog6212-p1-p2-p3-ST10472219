namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public int CategoryID { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    public string BibNumber { get; set; } = string.Empty;
    public string Status { get; set; } = EnrolmentStatuses.Confirmed;

    public Participant Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}
