namespace RaceDay.Api.Models;

public class Result
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = ResultStatuses.Finished;
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    public Enrolment Enrolment { get; set; } = null!;
}
