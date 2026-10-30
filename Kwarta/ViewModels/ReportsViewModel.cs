using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Models;
using Kwarta.Services;

namespace Kwarta.ViewModels;

/// <summary>One month in the income vs. spending chart. Heights are in device units.</summary>
public class TrendBar(DateTime month, decimal income, decimal expenses, decimal max, bool isSelected)
{
    const double ChartHeight = 140;

    public DateTime Month { get; } = month;
    public string Label { get; } = month.ToString("MMM");
    public bool IsSelected { get; } = isSelected;
    public double IncomeHeight { get; } = Height(income, max);
    public double ExpenseHeight { get; } = Height(expenses, max);
    public string Description { get; } =
        $"{month:MMMM yyyy}: income {Money.Format(income)}, spending {Money.Format(expenses)}";

    // A sliver for small non-zero amounts so they don't disappear.
    static double Height(decimal amount, decimal max) =>
        amount <= 0 || max <= 0 ? 0 : Math.Max(3, ChartHeight * (double)(amount / max));
}

/// <summary>A category's part of the month's spending.</summary>
public class CategoryShare(Category? category, decimal amount, decimal total)
{
    public string Name { get; } = category?.Name ?? "Uncategorized";
    public string Glyph { get; } = category?.Glyph ?? Icons.CategoryGlyph(null);
    public Color Color { get; } = category?.Color ?? AppColors.Muted;
    public Color Tint => Color.WithAlpha(0.16f);
    public string AmountText { get; } = Money.Format(amount);
    public double Share { get; } = total > 0 ? (double)(amount / total) : 0;
    public string ShareText => $"{Share:P0}";
}

public partial class ReportsViewModel(BudgetDatabase database) : MonthViewModel
{
    const int TrendMonths = 6;

    [ObservableProperty]
    public partial string SpentText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ChangeText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Color ChangeColor { get; set; } = AppColors.Muted;

    [ObservableProperty]
    public partial string ChangeGlyph { get; set; } = Icons.TrendingUp;

    [ObservableProperty]
    public partial string AverageText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SavingsRateText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasCategories { get; set; }

    public ObservableCollection<TrendBar> Trend { get; } = [];

    public ObservableCollection<CategoryShare> Categories { get; } = [];

    [RelayCommand]
    async Task SelectMonth(TrendBar bar)
    {
        Month = bar.Month;
        await LoadAsync();
    }

    protected override async Task LoadMonthAsync()
    {
        var first = Month.AddMonths(1 - TrendMonths);
        var categories = (await database.GetCategoriesAsync()).ToDictionary(c => c.Id);
        var transactions = await database.GetTransactionsAsync(first, MonthEnd);

        var months = Enumerable.Range(0, TrendMonths).Select(i => first.AddMonths(i)).ToList();
        var totals = months.ToDictionary(m => m, m =>
        {
            var inMonth = transactions.Where(t => BudgetMath.FirstOfMonth(t.Date) == m).ToList();
            return (Income: inMonth.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                Expenses: inMonth.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount));
        });

        var max = totals.Values.Max(t => Math.Max(t.Income, t.Expenses));
        Trend.Clear();
        foreach (var m in months)
            Trend.Add(new TrendBar(m, totals[m].Income, totals[m].Expenses, max, m == Month));

        var (income, spent) = totals[Month];
        var previous = totals[Month.AddMonths(-1)].Expenses;
        SpentText = Money.Format(spent);
        if (previous > 0)
        {
            var change = (spent - previous) / previous;
            ChangeText = change == 0 ? $"Same as {Month.AddMonths(-1):MMMM}"
                : $"{Math.Abs(change):P0} {(change > 0 ? "more" : "less")} than {Month.AddMonths(-1):MMMM}";
            ChangeColor = change > 0 ? AppColors.Expense : AppColors.Income;
            ChangeGlyph = change > 0 ? Icons.TrendingUp : Icons.TrendingDown;
        }
        else
        {
            ChangeText = $"No spending recorded in {Month.AddMonths(-1):MMMM}";
            ChangeColor = AppColors.Muted;
            ChangeGlyph = Icons.TrendingUp;
        }

        var activeMonths = totals.Values.Where(t => t.Expenses > 0).ToList();
        AverageText = activeMonths.Count == 0 ? Money.Format(0)
            : Money.Format(Math.Round(activeMonths.Average(t => t.Expenses), 2));
        SavingsRateText = income <= 0 ? "No income recorded this month"
            : income >= spent ? $"You kept {(income - spent) / income:P0} of your income"
            : $"You spent {Money.Format(spent - income)} more than you earned";

        var byCategory = transactions
            .Where(t => t.Type == TransactionType.Expense && BudgetMath.FirstOfMonth(t.Date) == Month)
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategoryShare(categories.GetValueOrDefault(g.Key), g.Sum(t => t.Amount), spent))
            .OrderByDescending(c => c.Share);
        Categories.Clear();
        foreach (var share in byCategory)
            Categories.Add(share);
        HasCategories = Categories.Count > 0;
    }
}
