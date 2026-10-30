using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Models;
using Kwarta.Services;
using Kwarta.Views;

namespace Kwarta.ViewModels;

/// <summary>An account with its current balance.</summary>
public class AccountItem(Account account, decimal balance)
{
    public Account Account { get; } = account;
    public decimal Balance { get; } = balance;

    public string Name => Account.Name;
    public string Glyph => Account.Glyph;
    public Color IconColor => Account.Color;
    public Color IconTint => Account.Tint;
    public string BalanceText => Money.Format(Balance);
    public bool IsNegative => Balance < 0;
}

/// <summary>Accounts of one type (Cash, E-wallets, Banks) with their combined balance.</summary>
public class AccountGroup(string title, IReadOnlyList<AccountItem> accounts)
{
    public string Title { get; } = title;
    public IReadOnlyList<AccountItem> Accounts { get; } = accounts;
    public string TotalText { get; } = Money.Format(accounts.Sum(a => a.Balance));
}

public partial class AccountsViewModel(BudgetDatabase database) : ObservableObject
{
    [ObservableProperty]
    public partial string TotalText { get; set; } = Money.Format(0);

    [ObservableProperty]
    public partial string SummaryText { get; set; } = string.Empty;

    public ObservableCollection<AccountGroup> Groups { get; } = [];

    public static string GroupTitle(AccountType type) => type switch
    {
        AccountType.EWallet => "E-wallets",
        AccountType.Bank => "Banks",
        _ => "Cash"
    };

    [RelayCommand]
    public async Task LoadAsync()
    {
        var accounts = await database.GetAccountsAsync();
        var netTotals = await database.GetAccountNetTotalsAsync();

        var items = accounts
            .Select(a => new AccountItem(a, a.StartingBalance + netTotals.GetValueOrDefault(a.Id)))
            .ToList();

        Groups.Clear();
        foreach (var group in items.GroupBy(i => i.Account.Type).OrderBy(g => g.Key))
            Groups.Add(new AccountGroup(GroupTitle(group.Key), group.ToList()));

        TotalText = Money.Format(items.Sum(i => i.Balance));
        SummaryText = $"Across {items.Count} account{(items.Count == 1 ? "" : "s")}";
    }

    [RelayCommand]
    Task AddAccount() => OpenEditorAsync(0);

    [RelayCommand]
    Task EditAccount(AccountItem item) => OpenEditorAsync(item.Account.Id);

    static Task OpenEditorAsync(int accountId) =>
        Shell.Current.GoToAsync(nameof(AccountEditPage), new Dictionary<string, object> { ["id"] = accountId });

    [RelayCommand]
    Task Transfer() =>
        Shell.Current.GoToAsync(nameof(TransactionEditPage),
            new Dictionary<string, object> { ["type"] = TransactionType.Transfer });
}
