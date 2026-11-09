namespace RaceDay.Mvc.Models;

public static class DisplayFormatting
{
    public static string FinishingPosition(int? position, int total)
    {
        if (position is null)
            return "—";

        var n = position.Value;
        return $"{n}{OrdinalSuffix(n)} out of {total} finishers";
    }

    private static string OrdinalSuffix(int n)
    {
        var lastTwo = n % 100;
        if (lastTwo is >= 11 and <= 13)
            return "th";

        return (n % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
    }
}
