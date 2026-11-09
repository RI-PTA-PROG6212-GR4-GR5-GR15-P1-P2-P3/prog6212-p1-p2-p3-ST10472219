using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Tests;

public class DisplayFormattingTests
{
    [Theory]
    [InlineData(1, 10, "1st out of 10 finishers")]
    [InlineData(2, 10, "2nd out of 10 finishers")]
    [InlineData(3, 10, "3rd out of 10 finishers")]
    [InlineData(4, 10, "4th out of 10 finishers")]
    [InlineData(11, 50, "11th out of 50 finishers")]
    [InlineData(12, 50, "12th out of 50 finishers")]
    [InlineData(13, 50, "13th out of 50 finishers")]
    [InlineData(21, 50, "21st out of 50 finishers")]
    [InlineData(47, 312, "47th out of 312 finishers")]
    public void FinishingPosition_UsesOrdinalsAndFinisherCount(int position, int total, string expected) =>
        Assert.Equal(expected, DisplayFormatting.FinishingPosition(position, total));

    [Fact]
    public void FinishingPosition_WithoutPosition_ShowsDash() =>
        Assert.Equal("—", DisplayFormatting.FinishingPosition(null, 12));
}
