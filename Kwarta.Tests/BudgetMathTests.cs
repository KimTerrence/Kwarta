using Kwarta.Services;

namespace Kwarta.Tests;

public class BudgetMathTests
{
    static readonly DateTime Jan = new(2026, 1, 1);
    static readonly DateTime Feb = new(2026, 2, 1);
    static readonly DateTime Mar = new(2026, 3, 1);
    static readonly DateTime Apr = new(2026, 4, 1);

    [Fact]
    public void Unspent_budget_accumulates_month_to_month()
    {
        var spent = new Dictionary<DateTime, decimal> { [Jan] = 800, [Feb] = 900 };

        // Jan leaves 200, Feb leaves 100 more.
        Assert.Equal(300, BudgetMath.Carryover(1000, Jan, spent, Mar));
    }

    [Fact]
    public void Overspending_uses_up_the_carry_but_not_below_zero()
    {
        var spent = new Dictionary<DateTime, decimal> { [Jan] = 500, [Feb] = 2000, [Mar] = 900 };

        // Jan +500, Feb -1000 → 0 (not -500), Mar +100.
        Assert.Equal(100, BudgetMath.Carryover(1000, Jan, spent, Apr));
    }

    [Fact]
    public void Months_without_spending_carry_the_whole_budget()
    {
        Assert.Equal(2000, BudgetMath.Carryover(1000, Jan, new Dictionary<DateTime, decimal>(), Mar));
    }

    [Fact]
    public void Nothing_carries_into_the_month_rollover_started()
    {
        Assert.Equal(0, BudgetMath.Carryover(1000, new DateTime(2026, 3, 15), new Dictionary<DateTime, decimal>(), Mar));
    }

    [Fact]
    public void No_budget_means_no_carry()
    {
        Assert.Equal(0, BudgetMath.Carryover(0, Jan, new Dictionary<DateTime, decimal>(), Apr));
    }
}
