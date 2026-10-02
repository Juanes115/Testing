using BaseTestingLab.Core.Common.Exceptions;

namespace BaseTestingLab.Core.DateTimeUtils;

public record DateRange
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public DateRange(DateTime start, DateTime end)
    {
        if (end < start)
        {
            throw new ValidationException(
                $"La fecha de fin ({end:yyyy-MM-dd HH:mm:ss}) no puede ser anterior a la de inicio ({start:yyyy-MM-dd HH:mm:ss}).",
                nameof(end));
        }

        Start = start;
        End = end;
    }

    public TimeSpan Duration => End - Start;

    public bool OverlapsWith(DateRange other)
    {
        if (other == null) return false;
        return Start < other.End && End > other.Start;
    }

    public bool Contains(DateTime dateTime)
    {
        return dateTime >= Start && dateTime <= End;
    }

    public bool Contains(DateRange other)
    {
        if (other == null) return false;
        return Start <= other.Start && End >= other.End;
    }

    public bool IsContiguousWith(DateRange other)
    {
        if (other == null) return false;
        return End == other.Start || Start == other.End;
    }
}
