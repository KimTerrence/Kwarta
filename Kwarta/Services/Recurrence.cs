using Kwarta.Models;

namespace Kwarta.Services;

/// <summary>Dates of a <see cref="RecurringTransaction"/>, counted from its start date.</summary>
public static class Recurrence
{
    /// <summary>The occurrence <paramref name="index"/> steps after <paramref name="start"/>. Month ends clamp (Jan 31 → Feb 28).</summary>
    public static DateTime Occurrence(DateTime start, RecurringFrequency frequency, int index) => frequency switch
    {
        RecurringFrequency.Weekly => start.Date.AddDays(7 * index),
        RecurringFrequency.Monthly => start.Date.AddMonths(index),
        _ => start.Date.AddYears(index),
    };

    /// <summary>First occurrence after <paramref name="date"/>.</summary>
    public static DateTime NextAfter(DateTime start, RecurringFrequency frequency, DateTime date)
    {
        start = start.Date;
        date = date.Date;
        if (date < start)
            return start;

        // Estimate the index, then step past any occurrence on or before the date.
        var index = frequency switch
        {
            RecurringFrequency.Weekly => (date - start).Days / 7,
            RecurringFrequency.Monthly => (date.Year - start.Year) * 12 + date.Month - start.Month,
            _ => date.Year - start.Year,
        };
        while (Occurrence(start, frequency, index) <= date)
            index++;
        return Occurrence(start, frequency, index);
    }

    /// <summary>Occurrences from <see cref="RecurringTransaction.NextDate"/> up to and including <paramref name="until"/>.</summary>
    public static IEnumerable<DateTime> DueDates(RecurringTransaction recurring, DateTime until)
    {
        for (var date = recurring.NextDate.Date; date <= until.Date;
             date = NextAfter(recurring.StartDate, recurring.Frequency, date))
            yield return date;
    }

    public static string Describe(RecurringFrequency frequency, DateTime start) => frequency switch
    {
        RecurringFrequency.Weekly => $"Every {start:dddd}",
        RecurringFrequency.Monthly => start.Day >= 29 ? $"Monthly on the {Ordinal(start.Day)} (or month end)"
            : $"Monthly on the {Ordinal(start.Day)}",
        _ => $"Yearly on {start:MMM d}",
    };

    static string Ordinal(int day) => day + (day is 11 or 12 or 13 ? "th" : (day % 10) switch
    {
        1 => "st",
        2 => "nd",
        3 => "rd",
        _ => "th",
    });
}
