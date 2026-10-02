using BaseTestingLab.Core.Common.Exceptions;
using BaseTestingLab.Core.DateTimeUtils;
using FluentAssertions;
using Xunit;

namespace BaseTestingLab.Tests.DateTimeUtils;

public class DateRangeTests
{
    [Fact]
    public void Constructor_WhenEndBeforeStart_ShouldThrowValidationException()
    {
        var start = new DateTime(2026, 10, 10);
        var end = new DateTime(2026, 10, 5);

        Action act = () => new DateRange(start, end);

        act.Should().Throw<ValidationException>().WithMessage("*no puede ser anterior*");
    }

    [Fact]
    public void Duration_ShouldCalculateCorrectTimeSpan()
    {
        var start = new DateTime(2026, 1, 1, 10, 0, 0);
        var end = new DateTime(2026, 1, 3, 14, 30, 0);
        var range = new DateRange(start, end);

        range.Duration.Should().Be(TimeSpan.FromHours(52.5));
    }

    [Theory]
    [InlineData("2026-05-15", "2026-05-25", true)]
    [InlineData("2026-05-01", "2026-05-12", true)]
    [InlineData("2026-05-12", "2026-05-18", true)]
    [InlineData("2026-05-01", "2026-05-10", false)]
    [InlineData("2026-05-20", "2026-05-30", false)]
    [InlineData("2026-06-01", "2026-06-10", false)]
    public void OverlapsWith_ShouldDetermineCollisionCorrectly(string otherStartStr, string otherEndStr, bool expectedOverlap)
    {
        var baseRange = new DateRange(DateTime.Parse("2026-05-10"), DateTime.Parse("2026-05-20"));
        var testRange = new DateRange(DateTime.Parse(otherStartStr), DateTime.Parse(otherEndStr));

        var overlaps = baseRange.OverlapsWith(testRange);
        overlaps.Should().Be(expectedOverlap);
    }

    [Fact]
    public void Contains_ShouldValidatePointInTime()
    {
        var range = new DateRange(DateTime.Parse("2026-01-01"), DateTime.Parse("2026-01-31"));

        range.Contains(DateTime.Parse("2026-01-15")).Should().BeTrue();
        range.Contains(DateTime.Parse("2026-02-01")).Should().BeFalse();
    }

    [Fact]
    public void IsContiguousWith_ShouldIdentifyAdjacentRanges()
    {
        var rangeA = new DateRange(DateTime.Parse("2026-01-01"), DateTime.Parse("2026-01-15"));
        var rangeB = new DateRange(DateTime.Parse("2026-01-15"), DateTime.Parse("2026-01-31"));

        rangeA.IsContiguousWith(rangeB).Should().BeTrue();
        rangeB.IsContiguousWith(rangeA).Should().BeTrue();
    }
}
