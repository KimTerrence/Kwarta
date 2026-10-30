using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Models;
using Kwarta.Services;
using Kwarta.Views;

namespace Kwarta.ViewModels;

/// <summary>A savings goal with how much is saved: the linked account's balance, or the amount entered by hand.</summary>
public class GoalItem(SavingsGoal goal, decimal saved, Account? account)
{
    public SavingsGoal Goal { get; } = goal;

    public string Name => Goal.Name;
    public Color Color => Goal.Color;
    public Color Tint => Goal.Color.WithAlpha(0.16f);
    public string Glyph => Icons.Savings;
    public bool IsReached => saved >= Goal.TargetAmount;
    public double Progress => Goal.TargetAmount > 0 ? (double)Math.Clamp(saved / Goal.TargetAmount, 0, 1) : 0;
    public string SavedText => $"{Money.Format(saved)} of {Money.Format(Goal.TargetAmount)}";
    public string StatusText => IsReached ? "Reached!" : $"{Money.Format(Goal.TargetAmount - saved)} to go";
    public Color StatusColor => IsReached ? AppColors.Income : AppColors.Muted;
    public string SourceText => account is null ? "Updated by hand" : $"Balance of {account.Name}";
}

public partial class GoalsViewModel(BudgetDatabase database) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    public ObservableCollection<GoalItem> Items { get; } = [];

    public async Task LoadAsync()
    {
        var accounts = (await database.GetAccountsAsync()).ToDictionary(a => a.Id);
        var balances = await database.GetAccountBalancesAsync();

        Items.Clear();
        foreach (var goal in (await database.GetAllAsync<SavingsGoal>()).OrderBy(g => g.Name))
        {
            var account = accounts.GetValueOrDefault(goal.AccountId);
            Items.Add(new GoalItem(goal, account is null ? goal.SavedAmount : balances.GetValueOrDefault(account.Id), account));
        }
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    Task Open(GoalItem item) => OpenEditorAsync(item.Goal.Id);

    [RelayCommand]
    Task Add() => OpenEditorAsync(0);

    static Task OpenEditorAsync(int id) =>
        Shell.Current.GoToAsync(nameof(GoalEditPage), new Dictionary<string, object> { ["id"] = id });
}

public partial class GoalEditViewModel(BudgetDatabase database) : ObservableObject, IQueryAttributable
{
    SavingsGoal _goal = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial bool IsExisting { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TargetText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SavedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLinked { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorText { get; set; } = string.Empty;

    public string Title => IsExisting ? "Edit Goal" : "New Goal";
    public bool HasError => !string.IsNullOrEmpty(ErrorText);
    public string CurrencySymbol => Money.Symbol;

    /// <summary>Accounts to link, plus "Not linked" (id 0).</summary>
    public ObservableCollection<ChipChoice> Accounts { get; } = [];

    public IReadOnlyList<SelectableOption> ColorOptions { get; } =
        Icons.CategoryColors.Select(hex => new SelectableOption(hex, string.Empty, Color.FromArgb(hex))).ToList();

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idValue) && idValue is int id && id != 0
            && await database.GetAsync<SavingsGoal>(id) is { } existing)
        {
            _goal = existing;
            IsExisting = true;
            Name = existing.Name;
            TargetText = existing.TargetAmount.ToString("0.##");
            SavedText = existing.SavedAmount > 0 ? existing.SavedAmount.ToString("0.##") : string.Empty;
        }

        Accounts.Clear();
        Accounts.Add(new ChipChoice(0, "Not linked", Icons.EditNote, AppColors.Muted));
        foreach (var account in await database.GetAccountsAsync())
            Accounts.Add(new ChipChoice(account));
        SelectAccount(Accounts.FirstOrDefault(a => a.Id == _goal.AccountId) ?? Accounts[0]);

        SelectColor(ColorOptions.FirstOrDefault(o => o.Key.Equals(_goal.ColorHex, StringComparison.OrdinalIgnoreCase))
            ?? ColorOptions[0]);
    }

    [RelayCommand]
    void SelectAccount(ChipChoice choice)
    {
        foreach (var a in Accounts)
            a.IsSelected = a == choice;
        IsLinked = choice.Id != 0;
    }

    [RelayCommand]
    void SelectColor(SelectableOption option)
    {
        foreach (var o in ColorOptions)
            o.IsSelected = o == option;
    }

    [RelayCommand]
    async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorText = "Give the goal a name.";
            return;
        }

        if (!Money.TryParse(TargetText, out var target) || target <= 0)
        {
            ErrorText = "Enter a target amount greater than zero.";
            return;
        }

        decimal saved = 0;
        if (!IsLinked && !string.IsNullOrWhiteSpace(SavedText) && (!Money.TryParse(SavedText, out saved) || saved < 0))
        {
            ErrorText = "Enter a valid amount saved, or leave it blank.";
            return;
        }

        _goal.Name = Name.Trim();
        _goal.TargetAmount = Math.Round(target, 2);
        _goal.AccountId = Accounts.First(a => a.IsSelected).Id;
        _goal.SavedAmount = IsLinked ? 0 : Math.Round(saved, 2);
        _goal.ColorHex = ColorOptions.First(o => o.IsSelected).Key;

        await database.SaveAsync(_goal);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    async Task Delete()
    {
        if (!IsExisting || !await Shell.Current.DisplayAlertAsync("Delete goal", $"Delete {_goal.Name}?", "Delete", "Cancel"))
            return;

        await database.DeleteAsync(_goal);
        await Shell.Current.GoToAsync("..");
    }
}
