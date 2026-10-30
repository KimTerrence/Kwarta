using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Models;
using Kwarta.Services;
using Kwarta.Views;

namespace Kwarta.ViewModels;

public static class RecurringActions
{
    /// <summary>
    /// Offers to record or skip a due occurrence that waits to be recorded by hand; otherwise opens the editor.
    /// Returns true if it skipped an occurrence, so the caller should reload.
    /// </summary>
    public static async Task<bool> OpenAsync(RecurringItem item, RecurringService recurringService)
    {
        if (!item.NeedsRecording)
        {
            await EditAsync(item.Recurring.Id);
            return false;
        }

        const string record = "Record it", skip = "Skip this time", edit = "Edit schedule";
        var choice = await Shell.Current.DisplayActionSheetAsync($"{item.Name} · {item.DueText}", "Cancel", null,
            record, skip, edit);
        switch (choice)
        {
            case record:
                await Shell.Current.GoToAsync(nameof(TransactionEditPage),
                    new Dictionary<string, object> { ["recordRecurringId"] = item.Recurring.Id });
                return false;
            case skip:
                await recurringService.AdvanceAsync(item.Recurring);
                return true;
            case edit:
                await EditAsync(item.Recurring.Id);
                return false;
            default:
                return false;
        }
    }

    public static Task EditAsync(int recurringId) =>
        Shell.Current.GoToAsync(nameof(TransactionEditPage), new Dictionary<string, object>
        {
            ["mode"] = EditorMode.Recurring,
            ["id"] = recurringId,
        });
}

public partial class RecurringListViewModel(BudgetDatabase database, RecurringService recurringService) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial string MonthlyTotalText { get; set; } = string.Empty;

    public ObservableCollection<RecurringItem> Items { get; } = [];

    public async Task LoadAsync()
    {
        await recurringService.ProcessDueAsync();

        var categories = (await database.GetCategoriesAsync()).ToDictionary(c => c.Id);
        var accounts = (await database.GetAccountsAsync()).ToDictionary(a => a.Id);
        var recurring = (await database.GetAllAsync<RecurringTransaction>()).OrderBy(r => r.NextDate).ToList();

        Items.Clear();
        foreach (var r in recurring)
            Items.Add(new RecurringItem(r, categories.GetValueOrDefault(r.CategoryId),
                accounts.GetValueOrDefault(r.AccountId), accounts.GetValueOrDefault(r.ToAccountId)));
        IsEmpty = Items.Count == 0;

        // Rough monthly cost of recurring expenses: weekly ones happen about 52/12 times a month.
        var monthly = recurring.Where(r => r.Type == TransactionType.Expense).Sum(r => r.Frequency switch
        {
            RecurringFrequency.Weekly => r.Amount * 52 / 12,
            RecurringFrequency.Monthly => r.Amount,
            _ => r.Amount / 12,
        });
        MonthlyTotalText = $"About {Money.Format(Math.Round(monthly, 2))} a month in recurring expenses";
    }

    [RelayCommand]
    async Task Open(RecurringItem item)
    {
        if (await RecurringActions.OpenAsync(item, recurringService))
            await LoadAsync();
    }

    [RelayCommand]
    Task Add() =>
        Shell.Current.GoToAsync(nameof(TransactionEditPage),
            new Dictionary<string, object> { ["mode"] = EditorMode.Recurring });
}

public partial class TemplatesViewModel(BudgetDatabase database) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    public ObservableCollection<TemplateItem> Items { get; } = [];

    public async Task LoadAsync()
    {
        var categories = (await database.GetCategoriesAsync()).ToDictionary(c => c.Id);
        var accounts = (await database.GetAccountsAsync()).ToDictionary(a => a.Id);

        Items.Clear();
        foreach (var t in await database.GetAllAsync<TransactionTemplate>())
            Items.Add(new TemplateItem(t, categories.GetValueOrDefault(t.CategoryId),
                accounts.GetValueOrDefault(t.AccountId), accounts.GetValueOrDefault(t.ToAccountId)));
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    Task Open(TemplateItem item) =>
        Shell.Current.GoToAsync(nameof(TransactionEditPage), new Dictionary<string, object>
        {
            ["mode"] = EditorMode.Template,
            ["id"] = item.Template.Id,
        });

    [RelayCommand]
    Task Add() =>
        Shell.Current.GoToAsync(nameof(TransactionEditPage),
            new Dictionary<string, object> { ["mode"] = EditorMode.Template });
}
