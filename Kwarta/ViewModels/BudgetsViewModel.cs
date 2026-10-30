using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Models;
using Kwarta.Services;

namespace Kwarta.ViewModels;

public partial class BudgetsViewModel(BudgetDatabase database) : MonthViewModel
{
    [ObservableProperty]
    public partial string TotalBudgetText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TotalSpentText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double TotalProgress { get; set; }

    [ObservableProperty]
    public partial Color TotalBarColor { get; set; } = AppColors.Income;

    public ObservableCollection<BudgetProgressItem> Budgets { get; } = [];

    public ObservableCollection<Category> IncomeCategories { get; } = [];

    protected override async Task LoadMonthAsync()
    {
        var categories = await database.GetCategoriesAsync();
        var transactions = await database.GetTransactionsAsync(Month, MonthEnd);
        var carryover = await database.GetBudgetCarryoverAsync(categories, Month);

        var spentByCategory = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        var budgetItems = categories
            .Where(c => c.Type == TransactionType.Expense)
            .Select(c => new BudgetProgressItem(c, spentByCategory.GetValueOrDefault(c.Id), carryover.GetValueOrDefault(c.Id)))
            .OrderByDescending(b => b.HasBudget)
            .ThenBy(b => b.Name);

        Budgets.Clear();
        foreach (var item in budgetItems)
            Budgets.Add(item);

        IncomeCategories.Clear();
        foreach (var category in categories.Where(c => c.Type == TransactionType.Income))
            IncomeCategories.Add(category);

        var totalBudget = Budgets.Sum(b => b.Budget);
        var totalSpent = Budgets.Where(b => b.HasBudget).Sum(b => b.Spent);
        TotalBudgetText = Money.Format(totalBudget);
        TotalSpentText = $"{Money.Format(totalSpent)} spent in budgeted categories";
        TotalProgress = totalBudget > 0 ? (double)Math.Min(1m, totalSpent / totalBudget) : 0;
        TotalBarColor = AppColors.ForProgress(TotalProgress, totalSpent > totalBudget);
    }

    [RelayCommand]
    Task EditBudget(BudgetProgressItem item) => OpenCategoryEditorAsync(item.Category.Id, item.Category.Type);

    [RelayCommand]
    Task EditIncomeCategory(Category category) => OpenCategoryEditorAsync(category.Id, category.Type);

    [RelayCommand]
    Task AddExpenseCategory() => OpenCategoryEditorAsync(0, TransactionType.Expense);

    [RelayCommand]
    Task AddIncomeCategory() => OpenCategoryEditorAsync(0, TransactionType.Income);

    static Task OpenCategoryEditorAsync(int categoryId, TransactionType type) =>
        Shell.Current.GoToAsync(nameof(Views.CategoryEditPage),
            new Dictionary<string, object> { ["id"] = categoryId, ["type"] = type });
}
