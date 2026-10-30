using Kwarta.Models;
using Kwarta.Services;

namespace Kwarta.ViewModels;

/// <summary>How much of a category's monthly budget has been used.</summary>
public class BudgetProgressItem(Category category, decimal spent)
{
    public Category Category { get; } = category;
    public decimal Spent { get; } = spent;

    public string Name => Category.Name;
    public string Glyph => Category.Glyph;
    public Color IconColor => Category.Color;
    public Color IconTint => Category.Tint;
    public bool HasBudget => Category.MonthlyBudget > 0;
    public bool IsOverBudget => HasBudget && Spent > Category.MonthlyBudget;

    public double Progress => HasBudget ? (double)Math.Min(1m, Spent / Category.MonthlyBudget) : 0;

    public string SpentText => HasBudget
        ? $"{Money.Format(Spent)} of {Money.Format(Category.MonthlyBudget)}"
        : Money.Format(Spent);

    public string StatusText => !HasBudget ? "No budget set"
        : IsOverBudget ? $"{Money.Format(Spent - Category.MonthlyBudget)} over"
        : $"{Money.Format(Category.MonthlyBudget - Spent)} left";

    public Color BarColor => AppColors.ForProgress(Progress, IsOverBudget);

    public Color StatusColor => IsOverBudget ? AppColors.Expense : AppColors.Muted;
}
