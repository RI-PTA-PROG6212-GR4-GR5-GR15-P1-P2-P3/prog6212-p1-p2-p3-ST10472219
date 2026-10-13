namespace RaceDay.Api;

/// <summary>The two roles in the RaceDay system.</summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

/// <summary>Keys used to store the authenticated user in the server-side session.</summary>
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
}

public static class EventStatuses
{
    public const string Scheduled = "Scheduled";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";
    public static readonly string[] All = { Scheduled, Cancelled, Completed };
}

public static class EventTypes
{
    public const string Running = "Running";
    public const string Cycling = "Cycling";
    public const string Walking = "Walking";

    /// <summary>Accepts "Running"/"run", "Cycling"/"cycle", "Walking"/"walk" (any casing).</summary>
    public static bool TryNormalise(string? input, out string value)
    {
        value = string.Empty;
        switch (input?.Trim().ToLowerInvariant())
        {
            case "running":
            case "run":
                value = Running;
                return true;
            case "cycling":
            case "cycle":
                value = Cycling;
                return true;
            case "walking":
            case "walk":
                value = Walking;
                return true;
            default:
                return false;
        }
    }
}

public static class EnrolmentStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
}

public static class ResultStatuses
{
    public const string Finished = "Finished";
    public const string DNF = "DNF";
    public const string DSQ = "DSQ";
    public static readonly string[] All = { Finished, DNF, DSQ };
}

public static class StatusMatcher
{
    /// <summary>Case-insensitive match against a list of allowed values; returns the canonical value.</summary>
    public static bool TryMatch(string? input, string[] allowed, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var match = allowed.FirstOrDefault(a => string.Equals(a, input.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null) return false;
        value = match;
        return true;
    }
}
