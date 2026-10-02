using BaseTestingLab.Core.Common.Providers;

namespace BaseTestingLab.Core.DateTimeUtils;

public class DateCalculator
{
    private readonly IDateTimeProvider _timeProvider;
    private readonly HolidayCalendar _holidayCalendar;

    public DateCalculator(IDateTimeProvider? timeProvider = null, HolidayCalendar? holidayCalendar = null)
    {
        _timeProvider = timeProvider ?? new SystemDateTimeProvider();
        _holidayCalendar = holidayCalendar ?? new HolidayCalendar();
    }

    public bool IsBusinessDay(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return false;

        return !_holidayCalendar.IsHoliday(date);
    }

    public DateOnly AddBusinessDays(DateOnly startDate, int businessDays)
    {
        if (businessDays == 0)
            return startDate;

        var current = startDate;
        int direction = businessDays > 0 ? 1 : -1;
        int remaining = Math.Abs(businessDays);

        while (remaining > 0)
        {
            current = current.AddDays(direction);
            if (IsBusinessDay(current))
            {
                remaining--;
            }
        }

        return current;
    }

    public int CountBusinessDays(DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate)
            (startDate, endDate) = (endDate, startDate);

        int count = 0;
        var current = startDate;

        while (current < endDate)
        {
            if (IsBusinessDay(current))
            {
                count++;
            }
            current = current.AddDays(1);
        }

        return count;
    }

    public (int Years, int Months, int Days) CalculateExactAge(DateOnly birthDate, DateOnly? asOfDate = null)
    {
        var targetDate = asOfDate ?? _timeProvider.Today;

        if (birthDate > targetDate)
            throw new ArgumentException("La fecha de nacimiento no puede ser posterior a la fecha de cálculo.", nameof(birthDate));

        int years = targetDate.Year - birthDate.Year;
        int months = targetDate.Month - birthDate.Month;
        int days = targetDate.Day - birthDate.Day;

        if (days < 0)
        {
            months--;
            var previousMonthDate = targetDate.AddMonths(-1);
            var daysInPreviousMonth = DateTime.DaysInMonth(previousMonthDate.Year, previousMonthDate.Month);
            days += daysInPreviousMonth;
        }

        if (months < 0)
        {
            years--;
            months += 12;
        }

        return (years, months, days);
    }

    public DateRange GetQuarterRange(int year, int quarter)
    {
        if (quarter is < 1 or > 4)
            throw new ArgumentOutOfRangeException(nameof(quarter), "El trimestre debe estar entre 1 y 4.");

        int startMonth = ((quarter - 1) * 3) + 1;
        int endMonth = startMonth + 2;

        var start = new DateTime(year, startMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        int daysInEndMonth = DateTime.DaysInMonth(year, endMonth);
        var end = new DateTime(year, endMonth, daysInEndMonth, 23, 59, 59, 999, DateTimeKind.Utc);

        return new DateRange(start, end);
    }

    public bool IsLeapYear(int year) => DateTime.IsLeapYear(year);
}
