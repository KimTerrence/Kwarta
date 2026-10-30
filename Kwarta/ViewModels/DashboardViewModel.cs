using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Models;
using Kwarta.Services;
using Kwarta.Views;

namespace Kwarta.ViewModels;

public partial class DashboardViewModel(BudgetDatabase database, RecurringService recurringService) : MonthViewModel
{
    const int RecentCount = 5;

    /// <summary>How far ahead the Upcoming card looks.</summary>
    const int UpcomingDays = 7;

    static readonly TimeSpan ToastDuration = TimeSpan.FromSeconds(5);

    CancellationTokenSource? _toastTimer;
    Transaction? _lastQuickAdd;

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

    [ObservableProperty]
    public partial bool HasUpcoming { get; set; }

    [ObservableProperty]
    public partial bool ShowBackupReminder { get; set; }

    [ObservableProperty]
    public partial string BackupReminderText { get; set; } = string.Empty;

    /// <summary>Confirmation after a quick add, with Undo.</summary>
    [ObservableProperty]
    public partial string ToastText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsToastVisible { get; set; }

    public ObservableCollection<BudgetProgressItem> Budgets { get; } = [];

    public ObservableCollection<TransactionItem> RecentTransactions { get; } = [];

    public ObservableCollection<TemplateItem> QuickAdds { get; } = [];

    public ObservableCollection<RecurringItem> Upcoming { get; } = [];

    [RelayCommand]
    Task ShowAllTransactions() => Shell.Current.GoToAsync("//transactions");

    [RelayCommand]
    Task ShowBudgets() => Shell.Current.GoToAsync("//budgets");

    [RelayCommand]
    Task ShowRecurring() => Shell.Current.GoToAsync(nameof(RecurringPage));

    [RelayCommand]
    Task ManageQuickAdds() => Shell.Current.GoToAsync(nameof(TemplatesPage));

    [RelayCommand]
    Task BackUpNow() => Shell.Current.GoToAsync(nameof(BackupPage));

    [RelayCommand]
    void DismissBackupReminder()
    {
        BackupService.SnoozeReminder();
        ShowBackupReminder = false;
    }

    [RelayCommand]
    async Task QuickAdd(TemplateItem item)
    {
        var transaction = RecurringService.ToTransaction(item.Template, DateTime.Today);
        await database.SaveTransactionAsync(transaction);
        _lastQuickAdd = transaction;
        await LoadAsync();
        await ShowToastAsync($"Added {item.Name} · {item.AmountText}");
    }

    [RelayCommand]
    async Task UndoQuickAdd()
    {
        IsToastVisible = false;
        if (_lastQuickAdd is null)
            return;
        await database.DeleteTransactionAsync(_lastQuickAdd);
        _lastQuickAdd = null;
        await LoadAsync();
    }

    async Task ShowToastAsync(string text)
    {
        _toastTimer?.Cancel();
        _toastTimer = new CancellationTokenSource();
        ToastText = text;
        IsToastVisible = true;
        try
        {
            await Task.Delay(ToastDuration, _toastTimer.Token);
            IsToastVisible = false;
            _lastQuickAdd = null;
        }
        catch (TaskCanceledException)
        {
            // A newer toast replaced this one.
        }
    }

    [RelayCommand]
    async Task OpenUpcoming(RecurringItem item)
    {
        if (await RecurringActions.OpenAsync(item, recurringService))
            await LoadAsync();
    }

    protected override async Task LoadMonthAsync()
    {
        await recurringService.ProcessDueAsync();

        var categoryList = await database.GetCategoriesAsync();
        var categories = categoryList.ToDictionary(c => c.Id);
        var accounts = (await database.GetAccountsAsync()).ToDictionary(a => a.Id);
        var transactions = await database.GetTransactionsAsync(Month, MonthEnd);
        var carryover = await database.GetBudgetCarryoverAsync(categoryList, Month);

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
            .Select(c => new BudgetProgressItem(c, spentByCategory.GetValueOrDefault(c.Id), carryover.GetValueOrDefault(c.Id)))
            .Where(b => b.HasBudget || b.Spent > 0)
            .OrderByDescending(b => b.IsOverBudget)
            .ThenByDescending(b => b.Progress)
            .ThenByDescending(b => b.Spent)
            .ToList();

        Budgets.Clear();
        foreach (var item in budgetItems)
            Budgets.Add(item);
        HasBudgets = Budgets.Count > 0;

        var totalBudget = budgetItems.Where(b => b.HasBudget).Sum(b => b.Budget);
        var budgetedSpent = Budgets.Where(b => b.HasBudget).Sum(b => b.Spent);
        BudgetSummaryText = totalBudget > 0
            ? $"{Money.Format(budgetedSpent)} of {Money.Format(totalBudget)} budget used"
            : "Set monthly limits in the Budgets tab";

        RecentTransactions.Clear();
        foreach (var t in transactions.Take(RecentCount))
            RecentTransactions.Add(new TransactionItem(t, categories.GetValueOrDefault(t.CategoryId),
                accounts.GetValueOrDefault(t.AccountId), accounts.GetValueOrDefault(t.ToAccountId)));
        HasTransactions = RecentTransactions.Count > 0;

        QuickAdds.Clear();
        foreach (var template in await database.GetAllAsync<TransactionTemplate>())
            QuickAdds.Add(new TemplateItem(template, categories.GetValueOrDefault(template.CategoryId),
                accounts.GetValueOrDefault(template.AccountId), accounts.GetValueOrDefault(template.ToAccountId)));

        Upcoming.Clear();
        var horizon = DateTime.Today.AddDays(UpcomingDays);
        foreach (var r in (await database.GetAllAsync<RecurringTransaction>())
                 .Where(r => r.NextDate.Date <= horizon).OrderBy(r => r.NextDate))
            Upcoming.Add(new RecurringItem(r, categories.GetValueOrDefault(r.CategoryId),
                accounts.GetValueOrDefault(r.AccountId), accounts.GetValueOrDefault(r.ToAccountId)));
        HasUpcoming = Upcoming.Count > 0;

        ShowBackupReminder = BackupService.ShouldRemind();
        BackupReminderText = BackupService.LastBackupAt is { } last
            ? $"Your last backup was {BackupService.Describe(last).ToLowerInvariant()}. Save a copy in case you lose your phone."
            : "You haven't backed up yet. Save a copy in case you lose your phone.";
    }
}
