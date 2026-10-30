using Kwarta.Models;
using Kwarta.Services;

namespace Kwarta.ViewModels;

/// <summary>How much of a category's monthly budget (plus any rolled-over amount) has been used.</summary>
public class BudgetProgressItem(Category category, decimal spent, decimal carryover = 0)
{
    public Category Category { get; } = category;
    public decimal Spent { get; } = spent;
    public decimal Carryover { get; } = carryover;

    /// <summary>This month's budget: the monthly amount plus what rolled over.</summary>
    public decimal Budget => Category.MonthlyBudget + Carryover;

    public string Name => Category.Name;
    public string Glyph => Category.Glyph;
    public Color IconColor => Category.Color;
    public Color IconTint => Category.Tint;
    public bool HasBudget => Category.MonthlyBudget > 0;
    public bool IsOverBudget => HasBudget && Spent > Budget;

    public double Progress => HasBudget ? (double)Math.Min(1m, Spent / Budget) : 0;

    public string SpentText => HasBudget
        ? $"{Money.Format(Spent)} of {Money.Format(Budget)}"
        : Money.Format(Spent);

    public bool HasCarryover => Carryover > 0;

    public string CarryoverText => $"Includes {Money.Format(Carryover)} rolled over";

    public string StatusText => !HasBudget ? "No budget set"
        : IsOverBudget ? $"{Money.Format(Spent - Budget)} over"
        : $"{Money.Format(Budget - Spent)} left";

    public Color BarColor => AppColors.ForProgress(Progress, IsOverBudget);

    public Color StatusColor => IsOverBudget ? AppColors.Expense : AppColors.Muted;
}
