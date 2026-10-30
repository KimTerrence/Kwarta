using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Services;

namespace Kwarta.ViewModels;

public partial class TransactionsViewModel(BudgetDatabase database) : MonthViewModel
{
    List<TransactionItem> _items = [];

    [ObservableProperty]
    public partial string SelectedFilter { get; set; } = "All";

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TotalText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    /// <summary>
    /// Day headers interleaved with their transactions. A flat list is used instead of a grouped
    /// CollectionView, which crashes on Windows when switching Shell tabs.
    /// </summary>
    public ObservableCollection<object> Rows { get; } = [];

    partial void OnSelectedFilterChanged(string value) => ApplyFilter();

    [RelayCommand]
    void SetFilter(string filter) => SelectedFilter = filter;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    protected override async Task LoadMonthAsync()
    {
        var categories = (await database.GetCategoriesAsync()).ToDictionary(c => c.Id);
        var accounts = (await database.GetAccountsAsync()).ToDictionary(a => a.Id);
        var transactions = await database.GetTransactionsAsync(Month, MonthEnd);
        _items = transactions.Select(t => new TransactionItem(t, categories.GetValueOrDefault(t.CategoryId),
            accounts.GetValueOrDefault(t.AccountId), accounts.GetValueOrDefault(t.ToAccountId))).ToList();
        ApplyFilter();
    }

    void ApplyFilter()
    {
        IEnumerable<TransactionItem> items = SelectedFilter switch
        {
            "Expenses" => _items.Where(i => i.IsExpense),
            "Income" => _items.Where(i => i.IsIncome),
            "Transfers" => _items.Where(i => i.IsTransfer),
            _ => _items
        };

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            items = items.Where(i =>
                i.CategoryName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.AccountName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (i.IsTransfer && i.ToAccountName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                i.Note.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = items.ToList();

        Rows.Clear();
        foreach (var day in filtered.GroupBy(i => i.Transaction.Date.Date))
        {
            Rows.Add(new TransactionDayHeader(day.Key, day.Sum(i => i.SignedAmount)));
            foreach (var item in day)
                Rows.Add(item);
        }

        var count = filtered.Count;
        TotalText = $"{count} transaction{(count == 1 ? "" : "s")} · Net {Money.Format(filtered.Sum(i => i.SignedAmount))}";
        IsEmpty = count == 0;
    }

    [RelayCommand]
    async Task DeleteTransaction(TransactionItem item)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync("Delete transaction",
            $"Delete {item.AmountText} ({item.CategoryName})?", "Delete", "Cancel");
        if (!confirmed)
            return;

        await database.DeleteTransactionAsync(item.Transaction);
        await LoadAsync();
    }
}
