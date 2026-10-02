namespace BaseTestingLab.Core.DateTimeUtils;

public class HolidayCalendar
{
    private readonly HashSet<DateOnly> _holidays = new();

    public HolidayCalendar()
    {
    }

    public HolidayCalendar(IEnumerable<DateOnly> initialHolidays)
    {
        foreach (var holiday in initialHolidays)
        {
            _holidays.Add(holiday);
        }
    }

    public void AddHoliday(DateOnly date) => _holidays.Add(date);

    public void AddHolidays(IEnumerable<DateOnly> dates)
    {
        foreach (var date in dates)
        {
            _holidays.Add(date);
        }
    }

    public bool IsHoliday(DateOnly date) => _holidays.Contains(date);

    public IReadOnlyCollection<DateOnly> GetAllHolidays() => _holidays.ToList().AsReadOnly();

    public void Clear() => _holidays.Clear();
}
