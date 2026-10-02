using BaseTestingLab.Core.Common.Providers;
using BaseTestingLab.Core.DateTimeUtils;
using FluentAssertions;
using Moq;
using Xunit;

namespace BaseTestingLab.Tests.DateTimeUtils;

public class DateCalculatorTests
{
    private readonly Mock<IDateTimeProvider> _mockTimeProvider = new();
    private readonly HolidayCalendar _holidayCalendar = new();

    [Fact]
    public void IsBusinessDay_WhenWeekend_ShouldReturnFalse()
    {
        var saturday = new DateOnly(2026, 10, 3);
        var sunday = new DateOnly(2026, 10, 4);
        var sut = new DateCalculator(_mockTimeProvider.Object, _holidayCalendar);

        sut.IsBusinessDay(saturday).Should().BeFalse();
        sut.IsBusinessDay(sunday).Should().BeFalse();
    }

    [Fact]
    public void IsBusinessDay_WhenHoliday_ShouldReturnFalse()
    {
        var holiday = new DateOnly(2026, 10, 12);
        _holidayCalendar.AddHoliday(holiday);
        var sut = new DateCalculator(_mockTimeProvider.Object, _holidayCalendar);

        sut.IsBusinessDay(holiday).Should().BeFalse();
    }

    [Fact]
    public void AddBusinessDays_ShouldSkipWeekendsAndHolidays()
    {
        var startDate = new DateOnly(2026, 10, 9);
        _holidayCalendar.AddHoliday(new DateOnly(2026, 10, 12));
        var sut = new DateCalculator(_mockTimeProvider.Object, _holidayCalendar);

        var result = sut.AddBusinessDays(startDate, 2);
        result.Should().Be(new DateOnly(2026, 10, 14));
    }

    [Fact]
    public void CountBusinessDays_ShouldReturnAccurateCount()
    {
        var start = new DateOnly(2026, 10, 5);
        var end = new DateOnly(2026, 10, 9);
        var sut = new DateCalculator(_mockTimeProvider.Object, _holidayCalendar);

        var count = sut.CountBusinessDays(start, end);
        count.Should().Be(4);
    }

    [Fact]
    public void CalculateExactAge_UsingMockedTimeProvider_ShouldReturnPreciseYearsMonthsDays()
    {
        _mockTimeProvider.Setup(tp => tp.Today).Returns(new DateOnly(2026, 10, 2));

        var birthDate = new DateOnly(1995, 5, 17);
        var sut = new DateCalculator(_mockTimeProvider.Object);

        var (years, months, days) = sut.CalculateExactAge(birthDate);

        years.Should().Be(31);
        months.Should().Be(4);
        days.Should().Be(15);

        _mockTimeProvider.Verify(tp => tp.Today, Times.Once);
    }

    [Fact]
    public void CalculateExactAge_WhenBirthDateInFuture_ShouldThrowArgumentException()
    {
        _mockTimeProvider.Setup(tp => tp.Today).Returns(new DateOnly(2026, 10, 2));
        var futureBirthDate = new DateOnly(2027, 1, 1);
        var sut = new DateCalculator(_mockTimeProvider.Object);

        Action act = () => sut.CalculateExactAge(futureBirthDate);
        act.Should().Throw<ArgumentException>().WithMessage("*no puede ser posterior*");
    }

    [Theory]
    [InlineData(2024, true)]
    [InlineData(2026, false)]
    [InlineData(2000, true)]
    [InlineData(1900, false)]
    public void IsLeapYear_ShouldCheckCorrectly(int year, bool expected)
    {
        var sut = new DateCalculator(_mockTimeProvider.Object);
        sut.IsLeapYear(year).Should().Be(expected);
    }

    [Fact]
    public void GetQuarterRange_ShouldReturnCorrectBoundsForQ3()
    {
        var sut = new DateCalculator(_mockTimeProvider.Object);
        var range = sut.GetQuarterRange(2026, 3);

        range.Start.Should().Be(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        range.End.Month.Should().Be(9);
        range.End.Day.Should().Be(30);
    }

    [Fact]
    public void AddBusinessDays_WithNegativeDays_ShouldMoveBackwardsSkippingWeekends()
    {
        var sut = new DateCalculator();
        var date = new DateOnly(2026, 10, 14);

        var result = sut.AddBusinessDays(date, -2);
        result.Should().Be(new DateOnly(2026, 10, 12));
    }

    [Fact]
    public void SystemDateTimeProvider_ShouldReturnCurrentSystemTime()
    {
        var provider = new SystemDateTimeProvider();

        provider.UtcNow.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        provider.Now.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
        provider.Today.Should().Be(DateOnly.FromDateTime(DateTime.Today));
    }
}
