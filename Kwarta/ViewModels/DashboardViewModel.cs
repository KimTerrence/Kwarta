using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Models;
using Kwarta.Services;

namespace Kwarta.ViewModels;

public partial class DashboardViewModel(BudgetDatabase database) : MonthViewModel
{
    const int RecentCount = 5;

    [ObservableProperty]
    public partial string IncomeText { get; set; } = Money.Format(0);

    [ObservableProperty]
    public partial string ExpensesText { get; set; } = Money.Format(0);

    [ObservableProperty]
    public partial string BalanceText { get; set; } = Money.Format(0);

    [ObservableProperty]
    public partial bool IsOverspent { get; set; }

    [ObservableProperty]
    public partial string BudgetSummaryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasTransactions { get; set; }

    [ObservableProperty]
    public partial bool HasBudgets { get; set; }

    public ObservableCollection<BudgetProgressItem> Budgets { get; } = [];

    public ObservableCollection<TransactionItem> RecentTransactions { get; } = [];

    [RelayCommand]
    Task ShowAllTransactions() => Shell.Current.GoToAsync("//transactions");

    [RelayCommand]
    Task ShowBudgets() => Shell.Current.GoToAsync("//budgets");

    protected override async Task LoadMonthAsync()
    {
        var categories = (await database.GetCategoriesAsync()).ToDictionary(c => c.Id);
        var accounts = (await database.GetAccountsAsync()).ToDictionary(a => a.Id);
        var transactions = await database.GetTransactionsAsync(Month, MonthEnd);

        var income = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expenses = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var balance = income - expenses;

        IncomeText = Money.Format(income);
        ExpensesText = Money.Format(expenses);
        BalanceText = Money.Format(balance);
        IsOverspent = balance < 0;

        var spentByCategory = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        var expenseCategories = categories.Values.Where(c => c.Type == TransactionType.Expense).ToList();
        var budgetItems = expenseCategories
            .Select(c => new BudgetProgressItem(c, spentByCategory.GetValueOrDefault(c.Id)))
            .Where(b => b.HasBudget || b.Spent > 0)
            .OrderByDescending(b => b.IsOverBudget)
            .ThenByDescending(b => b.Progress)
            .ThenByDescending(b => b.Spent);

        Budgets.Clear();
        foreach (var item in budgetItems)
            Budgets.Add(item);
        HasBudgets = Budgets.Count > 0;

        var totalBudget = expenseCategories.Sum(c => c.MonthlyBudget);
        var budgetedSpent = Budgets.Where(b => b.HasBudget).Sum(b => b.Spent);
        BudgetSummaryText = totalBudget > 0
            ? $"{Money.Format(budgetedSpent)} of {Money.Format(totalBudget)} budget used"
            : "Set monthly limits in the Budgets tab";

        RecentTransactions.Clear();
        foreach (var t in transactions.Take(RecentCount))
            RecentTransactions.Add(new TransactionItem(t, categories.GetValueOrDefault(t.CategoryId),
                accounts.GetValueOrDefault(t.AccountId), accounts.GetValueOrDefault(t.ToAccountId)));
        HasTransactions = RecentTransactions.Count > 0;
    }
}
