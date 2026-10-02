using BaseTestingLab.Core.DateTimeUtils;
using FluentAssertions;
using Xunit;

namespace BaseTestingLab.Tests.DateTimeUtils;

public class HolidayCalendarTests
{
    [Fact]
    public void HolidayCalendar_ShouldSupportAddingAndQueryingHolidays()
    {
        var initial = new[] { new DateOnly(2026, 1, 1), new DateOnly(2026, 5, 1) };
        var calendar = new HolidayCalendar(initial);

        calendar.AddHoliday(new DateOnly(2026, 12, 25));
        calendar.AddHolidays(new[] { new DateOnly(2026, 7, 20), new DateOnly(2026, 8, 7) });

        calendar.IsHoliday(new DateOnly(2026, 1, 1)).Should().BeTrue();
        calendar.IsHoliday(new DateOnly(2026, 12, 25)).Should().BeTrue();
        calendar.IsHoliday(new DateOnly(2026, 7, 20)).Should().BeTrue();
        calendar.IsHoliday(new DateOnly(2026, 10, 15)).Should().BeFalse();

        var all = calendar.GetAllHolidays();
        all.Should().HaveCount(5);

        calendar.Clear();
        calendar.GetAllHolidays().Should().BeEmpty();
    }
}
