using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Models;
using Kwarta.Services;

namespace Kwarta.ViewModels;

public partial class TransactionEditViewModel(BudgetDatabase database) : ObservableObject, IQueryAttributable
{
    const string LastAccountKey = "LastAccountId";
    const string LastToAccountKey = "LastToAccountId";

    Transaction _transaction = new();

    /// <summary>Which type the chips in <see cref="Categories"/> belong to.</summary>
    TransactionType _categoriesType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExpense))]
    [NotifyPropertyChangedFor(nameof(IsIncome))]
    [NotifyPropertyChangedFor(nameof(IsTransfer))]
    [NotifyPropertyChangedFor(nameof(AccountLabel))]
    public partial TransactionType Type { get; set; } = TransactionType.Expense;

    [ObservableProperty]
    public partial string AmountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedCategoryId { get; set; }

    [ObservableProperty]
    public partial int SelectedAccountId { get; set; }

    [ObservableProperty]
    public partial int SelectedToAccountId { get; set; }

    [ObservableProperty]
    public partial DateTime Date { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial bool IsExisting { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorText { get; set; } = string.Empty;

    public bool IsExpense => Type == TransactionType.Expense;
    public bool IsIncome => Type == TransactionType.Income;
    public bool IsTransfer => Type == TransactionType.Transfer;
    public bool HasError => !string.IsNullOrEmpty(ErrorText);
    public string Title => IsExisting ? "Edit Transaction" : "New Transaction";
    public string AccountLabel => IsTransfer ? "From account" : "Account";
    public string CurrencySymbol => Money.Symbol;

    public ObservableCollection<ChipChoice> Categories { get; } = [];

    public ObservableCollection<ChipChoice> Accounts { get; } = [];

    /// <summary>Destination choices for a transfer; separate chips so each list tracks its own selection.</summary>
    public ObservableCollection<ChipChoice> ToAccounts { get; } = [];

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var value) && value is int id
            && await database.GetTransactionAsync(id) is { } existing)
        {
            _transaction = existing;
            IsExisting = true;
            Type = existing.Type;
            AmountText = existing.Amount.ToString("0.##");
            Date = existing.Date;
            Note = existing.Note;
        }
        else if (query.TryGetValue("type", out var typeValue) && typeValue is TransactionType type)
            Type = type;

        // A transfer has no category; keep expense categories ready in case the type is switched.
        await LoadCategoriesAsync(IsTransfer ? TransactionType.Expense : Type, _transaction.CategoryId);
        await LoadAccountsAsync(
            IsExisting ? _transaction.AccountId : Preferences.Default.Get(LastAccountKey, 0),
            IsExisting && IsTransfer ? _transaction.ToAccountId : Preferences.Default.Get(LastToAccountKey, 0));
    }

    async Task LoadCategoriesAsync(TransactionType type, int selectedId)
    {
        _categoriesType = type;
        Categories.Clear();
        foreach (var category in await database.GetCategoriesAsync(type))
            Categories.Add(new ChipChoice(category));

        SelectCategory(Categories.FirstOrDefault(c => c.Id == selectedId) ?? Categories.FirstOrDefault());
    }

    async Task LoadAccountsAsync(int selectedId, int selectedToId)
    {
        var accounts = await database.GetAccountsAsync();

        Accounts.Clear();
        ToAccounts.Clear();
        foreach (var account in accounts)
        {
            Accounts.Add(new ChipChoice(account));
            ToAccounts.Add(new ChipChoice(account));
        }

        SelectAccount(Accounts.FirstOrDefault(a => a.Id == selectedId) ?? Accounts.FirstOrDefault());
        SelectToAccount(ToAccounts.FirstOrDefault(a => a.Id == selectedToId && a.Id != SelectedAccountId)
            ?? ToAccounts.FirstOrDefault(a => a.Id != SelectedAccountId));
    }

    [RelayCommand]
    void SelectCategory(ChipChoice? choice)
    {
        foreach (var c in Categories)
            c.IsSelected = c == choice;

        SelectedCategoryId = choice?.Id ?? 0;
    }

    [RelayCommand]
    void SelectAccount(ChipChoice? choice)
    {
        foreach (var a in Accounts)
            a.IsSelected = a == choice;

        SelectedAccountId = choice?.Id ?? 0;
    }

    [RelayCommand]
    void SelectToAccount(ChipChoice? choice)
    {
        foreach (var a in ToAccounts)
            a.IsSelected = a == choice;

        SelectedToAccountId = choice?.Id ?? 0;
    }

    [RelayCommand]
    Task SetExpense() => SetTypeAsync(TransactionType.Expense);

    [RelayCommand]
    Task SetIncome() => SetTypeAsync(TransactionType.Income);

    [RelayCommand]
    Task SetTransfer() => SetTypeAsync(TransactionType.Transfer);

    async Task SetTypeAsync(TransactionType type)
    {
        if (Type == type)
            return;

        Type = type;

        // Transfers don't use categories, so the loaded list is kept for when the type is switched back.
        if (type != TransactionType.Transfer && type != _categoriesType)
            await LoadCategoriesAsync(type, SelectedCategoryId);
    }

    [RelayCommand]
    async Task Save()
    {
        if (!Money.TryParse(AmountText, out var amount) || amount <= 0)
        {
            ErrorText = "Enter an amount greater than zero.";
            return;
        }

        if (!IsTransfer && SelectedCategoryId == 0)
        {
            ErrorText = "Choose a category.";
            return;
        }

        if (SelectedAccountId == 0)
        {
            ErrorText = IsTransfer ? "Choose the account to transfer from." : "Choose an account.";
            return;
        }

        if (IsTransfer && SelectedToAccountId == 0)
        {
            ErrorText = "Choose the account to transfer to.";
            return;
        }

        if (IsTransfer && SelectedToAccountId == SelectedAccountId)
        {
            ErrorText = "Choose two different accounts.";
            return;
        }

        ErrorText = string.Empty;
        _transaction.Amount = Math.Round(amount, 2);
        _transaction.Type = Type;
        _transaction.CategoryId = IsTransfer ? 0 : SelectedCategoryId;
        _transaction.AccountId = SelectedAccountId;
        _transaction.ToAccountId = IsTransfer ? SelectedToAccountId : 0;
        _transaction.Date = Date.Date;
        _transaction.Note = Note.Trim();

        await database.SaveTransactionAsync(_transaction);
        Preferences.Default.Set(LastAccountKey, SelectedAccountId);
        if (IsTransfer)
            Preferences.Default.Set(LastToAccountKey, SelectedToAccountId);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    async Task Delete()
    {
        if (!IsExisting)
            return;

        if (!await Shell.Current.DisplayAlertAsync("Delete transaction", "Delete this transaction?", "Delete", "Cancel"))
            return;

        await database.DeleteTransactionAsync(_transaction);
        await Shell.Current.GoToAsync("..");
    }
}
