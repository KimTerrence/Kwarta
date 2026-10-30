using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Models;
using Kwarta.Services;

namespace Kwarta.ViewModels;

public partial class AccountEditViewModel(BudgetDatabase database) : ObservableObject, IQueryAttributable
{
    Account _account = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial bool IsExisting { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Glyph))]
    [NotifyPropertyChangedFor(nameof(IsCash))]
    [NotifyPropertyChangedFor(nameof(IsEWallet))]
    [NotifyPropertyChangedFor(nameof(IsBank))]
    public partial AccountType Type { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewName))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StartingBalanceText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Tint))]
    public partial Color Color { get; set; } = Color.FromArgb(Icons.CategoryColors[0]);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BalanceText { get; set; } = string.Empty;

    public string Title => IsExisting ? "Edit Account" : "New Account";
    public string Glyph => Icons.AccountGlyph(Type);
    public Color Tint => Color.WithAlpha(0.16f);
    public string PreviewName => string.IsNullOrWhiteSpace(Name) ? "Account name" : Name;
    public string CurrencySymbol => Money.Symbol;
    public bool HasError => !string.IsNullOrEmpty(ErrorText);
    public bool IsCash => Type == AccountType.Cash;
    public bool IsEWallet => Type == AccountType.EWallet;
    public bool IsBank => Type == AccountType.Bank;

    public IReadOnlyList<SelectableOption> ColorOptions { get; } =
        Icons.CategoryColors.Select(hex => new SelectableOption(hex, string.Empty, Color.FromArgb(hex))).ToList();

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idValue) && idValue is int id && id != 0
            && await database.GetAccountAsync(id) is { } existing)
        {
            _account = existing;
            IsExisting = true;
            BalanceText = Money.Format(await GetBalanceAsync());
        }
        else
        {
            _account = new Account
            {
                Type = AccountType.Bank,
                ColorHex = Icons.CategoryColors[Random.Shared.Next(Icons.CategoryColors.Count)]
            };
        }

        Type = _account.Type;
        Name = _account.Name;
        StartingBalanceText = _account.StartingBalance != 0 ? _account.StartingBalance.ToString("0.##") : string.Empty;
        SelectColor(ColorOptions.FirstOrDefault(o => o.Key.Equals(_account.ColorHex, StringComparison.OrdinalIgnoreCase))
            ?? ColorOptions[0]);
    }

    [RelayCommand]
    void SetCash() => Type = AccountType.Cash;

    [RelayCommand]
    void SetEWallet() => Type = AccountType.EWallet;

    [RelayCommand]
    void SetBank() => Type = AccountType.Bank;

    [RelayCommand]
    void SelectColor(SelectableOption option)
    {
        foreach (var o in ColorOptions)
            o.IsSelected = o == option;
        Color = option.Color;
    }

    [RelayCommand]
    async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorText = "Give the account a name.";
            return;
        }

        decimal startingBalance = 0;
        if (!string.IsNullOrWhiteSpace(StartingBalanceText) && !Money.TryParse(StartingBalanceText, out startingBalance))
        {
            ErrorText = "Enter a valid starting balance, or leave it blank.";
            return;
        }

        _account.Name = Name.Trim();
        _account.Type = Type;
        _account.ColorHex = ColorOptions.First(o => o.IsSelected).Key;
        _account.StartingBalance = Math.Round(startingBalance, 2);

        await database.SaveAccountAsync(_account);
        await Shell.Current.GoToAsync("..");
    }

    async Task<decimal> GetBalanceAsync() =>
        (await database.GetAccountBalancesAsync()).GetValueOrDefault(_account.Id);

    /// <summary>Records an adjustment so the balance matches what the bank or wallet actually shows.</summary>
    [RelayCommand]
    async Task AdjustBalance()
    {
        if (!IsExisting)
            return;

        var current = await GetBalanceAsync();
        var text = await Shell.Current.DisplayPromptAsync("Adjust balance",
            $"Kwarta shows {Money.Format(current)}. Enter the actual balance of {_account.Name}.",
            "Adjust", "Cancel", initialValue: current.ToString("0.##"), keyboard: Keyboard.Numeric);
        if (text is null)
            return;
        if (!Money.TryParse(text, out var actual))
        {
            await Shell.Current.DisplayAlertAsync("Adjust balance", "Enter a valid amount.", "OK");
            return;
        }

        var difference = Math.Round(actual - current, 2);
        if (difference == 0)
            return;

        await database.SaveTransactionAsync(new Transaction
        {
            Type = TransactionType.Adjustment,
            Amount = difference,
            AccountId = _account.Id,
            Date = DateTime.Today,
        });
        BalanceText = Money.Format(await GetBalanceAsync());
    }

    [RelayCommand]
    async Task Delete()
    {
        if (!IsExisting)
            return;

        if (await database.IsAccountInUseAsync(_account.Id))
        {
            await Shell.Current.DisplayAlertAsync("Account in use",
                $"{_account.Name} has transactions. Move or delete them before deleting the account.", "OK");
            return;
        }

        if (!await Shell.Current.DisplayAlertAsync("Delete account", $"Delete {_account.Name}?", "Delete", "Cancel"))
            return;

        await database.DeleteAccountAsync(_account);
        await Shell.Current.GoToAsync("..");
    }
}
