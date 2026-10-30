namespace Kwarta.Services;

public static class BudgetMath
{
    /// <summary>
    /// Unspent budget carried into <paramref name="month"/>. Each month from <paramref name="since"/> adds its
    /// leftover (budget plus carry, minus spending) to the next. Overspending uses up the carry but doesn't go below zero.
    /// </summary>
    /// <param name="spentByMonth">Spending keyed by the first day of each month.</param>
    public static decimal Carryover(
        decimal monthlyBudget, DateTime since, IReadOnlyDictionary<DateTime, decimal> spentByMonth, DateTime month)
    {
        if (monthlyBudget <= 0)
            return 0;

        decimal carry = 0;
        for (var m = FirstOfMonth(since); m < FirstOfMonth(month); m = m.AddMonths(1))
            carry = Math.Max(0, carry + monthlyBudget - spentByMonth.GetValueOrDefault(m));
        return carry;
    }

    public static DateTime FirstOfMonth(DateTime date) => new(date.Year, date.Month, 1);
}
