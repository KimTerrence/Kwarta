using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Models;
using Kwarta.Services;
using Kwarta.Views;

namespace Kwarta.ViewModels;

public class DebtItem(Debt debt)
{
    public Debt Debt { get; } = debt;

    public string Person => Debt.Person;
    public string Initial => Debt.Person.Trim() is { Length: > 0 } name ? name[..1].ToUpperInvariant() : "?";
    public Color Color => Debt.IsSettled ? AppColors.Muted : Debt.Direction == DebtDirection.Lent ? AppColors.Income : AppColors.Expense;
    public Color Tint => Color.WithAlpha(0.16f);
    public string AmountText => Money.Format(Debt.IsSettled ? Debt.Amount : Debt.Remaining);

    public string DetailText
    {
        get
        {
            var verb = Debt.Direction == DebtDirection.Lent ? "Lent" : "Borrowed";
            var paid = Debt.PaidAmount > 0 && !Debt.IsSettled ? $" · {Money.Format(Debt.PaidAmount)} paid" : string.Empty;
            var note = string.IsNullOrWhiteSpace(Debt.Note) ? string.Empty : $" · {Debt.Note}";
            return $"{verb} {Debt.Date:MMM d, yyyy}{paid}{note}";
        }
    }
}

public class DebtGroup(string title, string totalText, IReadOnlyList<DebtItem> items)
{
    public string Title { get; } = title;
    public string TotalText { get; } = totalText;
    public IReadOnlyList<DebtItem> Items { get; } = items;
}

public partial class DebtsViewModel(BudgetDatabase database) : ObservableObject
{
    [ObservableProperty]
    public partial string OwedToYouText { get; set; } = Money.Format(0);

    [ObservableProperty]
    public partial string YouOweText { get; set; } = Money.Format(0);

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    public ObservableCollection<DebtGroup> Groups { get; } = [];

    public async Task LoadAsync()
    {
        var debts = (await database.GetAllAsync<Debt>()).OrderByDescending(d => d.Date).ToList();
        var lent = debts.Where(d => !d.IsSettled && d.Direction == DebtDirection.Lent).ToList();
        var borrowed = debts.Where(d => !d.IsSettled && d.Direction == DebtDirection.Borrowed).ToList();
        var settled = debts.Where(d => d.IsSettled).ToList();

        OwedToYouText = Money.Format(lent.Sum(d => d.Remaining));
        YouOweText = Money.Format(borrowed.Sum(d => d.Remaining));

        Groups.Clear();
        if (lent.Count > 0)
            Groups.Add(new DebtGroup("Owed to you", OwedToYouText, lent.Select(d => new DebtItem(d)).ToList()));
        if (borrowed.Count > 0)
            Groups.Add(new DebtGroup("You owe", YouOweText, borrowed.Select(d => new DebtItem(d)).ToList()));
        if (settled.Count > 0)
            Groups.Add(new DebtGroup("Settled", string.Empty, settled.Select(d => new DebtItem(d)).ToList()));
        IsEmpty = debts.Count == 0;
    }

    [RelayCommand]
    Task Open(DebtItem item) => OpenEditorAsync(item.Debt.Id);

    [RelayCommand]
    Task Add() => OpenEditorAsync(0);

    static Task OpenEditorAsync(int id) =>
        Shell.Current.GoToAsync(nameof(DebtEditPage), new Dictionary<string, object> { ["id"] = id });
}

public partial class DebtEditViewModel(BudgetDatabase database) : ObservableObject, IQueryAttributable
{
    Debt _debt = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(CanRecordPayment))]
    public partial bool IsExisting { get; set; }

    [ObservableProperty]
    public partial string Person { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AmountText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRecordPayment))]
    public partial string PaidText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime Date { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorText { get; set; } = string.Empty;

    public PillGroup Direction { get; } = new(
        (nameof(DebtDirection.Lent), "They owe me"),
        (nameof(DebtDirection.Borrowed), "I owe them"));

    public string Title => IsExisting ? "Edit Utang" : "New Utang";
    public bool HasError => !string.IsNullOrEmpty(ErrorText);
    public string CurrencySymbol => Money.Symbol;
    public bool CanRecordPayment => IsExisting && !_debt.IsSettled;

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idValue) && idValue is int id && id != 0
            && await database.GetAsync<Debt>(id) is { } existing)
        {
            _debt = existing;
            Person = existing.Person;
            AmountText = existing.Amount.ToString("0.##");
            PaidText = existing.PaidAmount > 0 ? existing.PaidAmount.ToString("0.##") : string.Empty;
            Date = existing.Date;
            Note = existing.Note;
            Direction.SelectedKey = existing.Direction.ToString();
            IsExisting = true;
        }
    }

    /// <summary>Validates the form into the debt. Returns false (with <see cref="ErrorText"/> set) if it's invalid.</summary>
    bool Apply()
    {
        if (string.IsNullOrWhiteSpace(Person))
        {
            ErrorText = "Enter who it's with.";
            return false;
        }

        if (!Money.TryParse(AmountText, out var amount) || amount <= 0)
        {
            ErrorText = "Enter an amount greater than zero.";
            return false;
        }

        decimal paid = 0;
        if (!string.IsNullOrWhiteSpace(PaidText) && (!Money.TryParse(PaidText, out paid) || paid < 0 || paid > amount))
        {
            ErrorText = "The amount paid can't be more than the amount owed.";
            return false;
        }

        ErrorText = string.Empty;
        _debt.Person = Person.Trim();
        _debt.Direction = Enum.Parse<DebtDirection>(Direction.SelectedKey);
        _debt.Amount = Math.Round(amount, 2);
        _debt.PaidAmount = Math.Round(paid, 2);
        _debt.Date = Date.Date;
        _debt.Note = Note.Trim();
        return true;
    }

    [RelayCommand]
    async Task Save()
    {
        if (!Apply())
            return;
        await database.SaveAsync(_debt);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    async Task RecordPayment()
    {
        if (!Apply())
            return;

        var text = await Shell.Current.DisplayPromptAsync("Record payment",
            $"How much was paid? {Money.Format(_debt.Remaining)} is left.", "Record", "Cancel",
            initialValue: _debt.Remaining.ToString("0.##"), keyboard: Keyboard.Numeric);
        if (text is null)
            return;
        if (!Money.TryParse(text, out var payment) || payment <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Record payment", "Enter an amount greater than zero.", "OK");
            return;
        }

        _debt.PaidAmount = Math.Min(_debt.Amount, _debt.PaidAmount + Math.Round(payment, 2));
        await database.SaveAsync(_debt);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    async Task Delete()
    {
        if (!IsExisting || !await Shell.Current.DisplayAlertAsync("Delete utang",
                $"Delete the record with {_debt.Person}?", "Delete", "Cancel"))
            return;

        await database.DeleteAsync(_debt);
        await Shell.Current.GoToAsync("..");
    }
}
