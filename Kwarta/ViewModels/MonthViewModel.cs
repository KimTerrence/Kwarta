using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Services;
using Kwarta.Views;

namespace Kwarta.ViewModels;

/// <summary>Base for screens that show one month of data at a time.</summary>
public abstract partial class MonthViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthTitle))]
    [NotifyPropertyChangedFor(nameof(MonthEnd))]
    public partial DateTime Month { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public string MonthTitle => Month.ToString("MMMM yyyy");

    public DateTime MonthEnd => Month.AddMonths(1);

    [RelayCommand]
    Task PreviousMonth()
    {
        Month = Month.AddMonths(-1);
        return LoadAsync();
    }

    [RelayCommand]
    Task NextMonth()
    {
        Month = Month.AddMonths(1);
        return LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            await LoadMonthAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected abstract Task LoadMonthAsync();

    [RelayCommand]
    Task AddTransaction() => OpenEditorAsync(0);

    [RelayCommand]
    async Task EditTransaction(TransactionItem item)
    {
        if (!item.IsAdjustment)
        {
            await OpenEditorAsync(item.Transaction.Id);
            return;
        }

        // An adjustment has no editor; it can only be undone.
        if (await Shell.Current.DisplayAlertAsync("Balance adjustment",
                $"{item.AmountText} to {item.AccountName} on {item.DateText}. Delete it to undo the adjustment.",
                "Delete", "Close"))
        {
            await IPlatformApplication.Current!.Services.GetRequiredService<BudgetDatabase>()
                .DeleteTransactionAsync(item.Transaction);
            await LoadAsync();
        }
    }

    static Task OpenEditorAsync(int transactionId) =>
        Shell.Current.GoToAsync(nameof(TransactionEditPage),
            new Dictionary<string, object> { ["id"] = transactionId });
}
