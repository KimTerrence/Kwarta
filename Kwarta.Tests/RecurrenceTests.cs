using Kwarta.Models;
using Kwarta.Services;

namespace Kwarta.Tests;

public class RecurrenceTests
{
    [Fact]
    public void Monthly_on_the_31st_clamps_to_short_months_and_returns_to_the_31st()
    {
        var start = new DateTime(2026, 1, 31);

        Assert.Equal(new DateTime(2026, 2, 28), Recurrence.NextAfter(start, RecurringFrequency.Monthly, start));
        Assert.Equal(new DateTime(2026, 3, 31),
            Recurrence.NextAfter(start, RecurringFrequency.Monthly, new DateTime(2026, 2, 28)));
        Assert.Equal(new DateTime(2026, 4, 30),
            Recurrence.NextAfter(start, RecurringFrequency.Monthly, new DateTime(2026, 3, 31)));
    }

    [Fact]
    public void NextAfter_a_date_before_the_start_is_the_start()
    {
        var start = new DateTime(2026, 5, 15);
        Assert.Equal(start, Recurrence.NextAfter(start, RecurringFrequency.Weekly, new DateTime(2026, 1, 1)));
    }

    [Theory]
    [InlineData(RecurringFrequency.Weekly, "2026-03-09", "2026-03-16")]
    [InlineData(RecurringFrequency.Weekly, "2026-03-12", "2026-03-16")]
    [InlineData(RecurringFrequency.Monthly, "2026-03-09", "2026-04-02")]
    [InlineData(RecurringFrequency.Yearly, "2026-03-02", "2027-03-02")]
    public void NextAfter_is_the_first_occurrence_strictly_after_the_date(
        RecurringFrequency frequency, string date, string expected)
    {
        var start = new DateTime(2026, 3, 2);
        Assert.Equal(DateTime.Parse(expected), Recurrence.NextAfter(start, frequency, DateTime.Parse(date)));
    }

    [Fact]
    public void Yearly_on_leap_day_falls_on_Feb_28_in_other_years()
    {
        var start = new DateTime(2028, 2, 29);
        Assert.Equal(new DateTime(2029, 2, 28), Recurrence.NextAfter(start, RecurringFrequency.Yearly, start));
        Assert.Equal(new DateTime(2032, 2, 29),
            Recurrence.NextAfter(start, RecurringFrequency.Yearly, new DateTime(2031, 3, 1)));
    }

    [Fact]
    public void DueDates_lists_every_missed_occurrence_up_to_today()
    {
        var recurring = new RecurringTransaction
        {
            Frequency = RecurringFrequency.Monthly,
            StartDate = new DateTime(2026, 1, 15),
            NextDate = new DateTime(2026, 2, 15),
        };

        var due = Recurrence.DueDates(recurring, new DateTime(2026, 4, 20)).ToList();

        Assert.Equal([new DateTime(2026, 2, 15), new DateTime(2026, 3, 15), new DateTime(2026, 4, 15)], due);
    }

    [Fact]
    public void DueDates_is_empty_when_the_next_date_is_in_the_future()
    {
        var recurring = new RecurringTransaction { StartDate = new DateTime(2026, 5, 1), NextDate = new DateTime(2026, 5, 1) };
        Assert.Empty(Recurrence.DueDates(recurring, new DateTime(2026, 4, 30)));
    }

    [Theory]
    [InlineData(1, "Monthly on the 1st")]
    [InlineData(2, "Monthly on the 2nd")]
    [InlineData(3, "Monthly on the 3rd")]
    [InlineData(11, "Monthly on the 11th")]
    [InlineData(22, "Monthly on the 22nd")]
    [InlineData(30, "Monthly on the 30th (or month end)")]
    public void Describe_monthly_uses_ordinals(int day, string expected) =>
        Assert.Equal(expected, Recurrence.Describe(RecurringFrequency.Monthly, new DateTime(2026, 1, day)));
}
