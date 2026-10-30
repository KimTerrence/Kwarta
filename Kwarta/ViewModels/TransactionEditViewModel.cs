using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Models;
using Kwarta.Services;

namespace Kwarta.ViewModels;

/// <summary>What the transaction editor is filling in.</summary>
public enum EditorMode
{
    Transaction,

    /// <summary>A <see cref="TransactionTemplate"/> for quick add; it has no date.</summary>
    Template,

    /// <summary>A <see cref="RecurringTransaction"/>; the date is its next occurrence.</summary>
    Recurring
}

/// <summary>
/// Edits a transaction, quick-add template or recurring transaction. Query parameters: "mode" (<see cref="EditorMode"/>),
/// "id", "type" (<see cref="TransactionType"/> for a new one) and "recordRecurringId" (record a due bill as a transaction).
/// </summary>
public partial class TransactionEditViewModel(BudgetDatabase database, RecurringService recurringService)
    : ObservableObject, IQueryAttributable
{
    const string LastAccountKey = "LastAccountId";
    const string LastToAccountKey = "LastToAccountId";

    Transaction _transaction = new();
    TransactionTemplate _template = new();
    RecurringTransaction _recurring = new();

    /// <summary>The recurring transaction whose due occurrence is being recorded, if any.</summary>
    RecurringTransaction? _recordFrom;

    /// <summary>Which type the chips in <see cref="Categories"/> belong to.</summary>
    TransactionType _categoriesType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(IsRecurring))]
    [NotifyPropertyChangedFor(nameof(HasDate))]
    [NotifyPropertyChangedFor(nameof(DeleteText))]
    [NotifyPropertyChangedFor(nameof(NotePlaceholder))]
    public partial EditorMode Mode { get; set; }

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
    [NotifyPropertyChangedFor(nameof(ScheduleText))]
    public partial DateTime Date { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial bool IsExisting { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial bool IsRecording { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorText { get; set; } = string.Empty;

    /// <summary>Record each occurrence automatically (recurring only).</summary>
    [ObservableProperty]
    public partial bool AutoAdd { get; set; } = true;

    public PillGroup Frequency { get; } = new(
        (nameof(RecurringFrequency.Weekly), "Weekly"),
        (nameof(RecurringFrequency.Monthly), "Monthly"),
        (nameof(RecurringFrequency.Yearly), "Yearly"));

    /// <summary>Days before the due date to send a reminder; the key is <see cref="RecurringTransaction.RemindDaysBefore"/>.</summary>
    public PillGroup Reminder { get; } = new(
        ("-1", "No reminder"),
        ("0", "On the day"),
        ("1", "1 day before"),
        ("3", "3 days before"));

    public bool IsExpense => Type == TransactionType.Expense;
    public bool IsIncome => Type == TransactionType.Income;
    public bool IsTransfer => Type == TransactionType.Transfer;
    public bool IsRecurring => Mode == EditorMode.Recurring;
    public bool HasDate => Mode != EditorMode.Template;
    public bool HasError => !string.IsNullOrEmpty(ErrorText);
    public bool AreRemindersSupported => Reminders.IsSupported;
    public string AccountLabel => IsTransfer ? "From account" : "Account";
    public string CurrencySymbol => Money.Symbol;
    public string ScheduleText => Recurrence.Describe(SelectedFrequency, Date);

    public string NotePlaceholder => Mode == EditorMode.Transaction
        ? "Add a note (optional)"
        : "Name, e.g. Meralco or Jeep fare (optional)";

    public string Title => (Mode, IsExisting) switch
    {
        (EditorMode.Template, false) => "New Quick-add",
        (EditorMode.Template, true) => "Edit Quick-add",
        (EditorMode.Recurring, false) => "New Recurring",
        (EditorMode.Recurring, true) => "Edit Recurring",
        _ when IsRecording => "Record Bill",
        (_, false) => "New Transaction",
        (_, true) => "Edit Transaction",
    };

    public string DeleteText => Mode switch
    {
        EditorMode.Template => "Delete quick-add",
        EditorMode.Recurring => "Delete recurring transaction",
        _ => "Delete transaction",
    };

    RecurringFrequency SelectedFrequency => Enum.Parse<RecurringFrequency>(Frequency.SelectedKey);

    public ObservableCollection<ChipChoice> Categories { get; } = [];

    public ObservableCollection<ChipChoice> Accounts { get; } = [];

    /// <summary>Destination choices for a transfer; separate chips so each list tracks its own selection.</summary>
    public ObservableCollection<ChipChoice> ToAccounts { get; } = [];

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Frequency.PropertyChanged += (_, _) => OnPropertyChanged(nameof(ScheduleText));
        Frequency.SelectedKey = nameof(RecurringFrequency.Monthly);

        if (query.TryGetValue("mode", out var modeValue) && modeValue is EditorMode mode)
            Mode = mode;
        var id = query.TryGetValue("id", out var idValue) && idValue is int i ? i : 0;

        // The row whose fields fill the form, when editing or recording.
        ITransactionFields? source = null;
        switch (Mode)
        {
            case EditorMode.Template when await database.GetAsync<TransactionTemplate>(id) is { } template:
                source = _template = template;
                break;

            case EditorMode.Recurring when await database.GetAsync<RecurringTransaction>(id) is { } recurring:
                source = _recurring = recurring;
                Date = recurring.NextDate;
                Frequency.SelectedKey = recurring.Frequency.ToString();
                AutoAdd = recurring.AutoAdd;
                Reminder.SelectedKey = recurring.RemindDaysBefore.ToString();
                break;

            case EditorMode.Transaction when await database.GetTransactionAsync(id) is { } existing:
                source = _transaction = existing;
                Date = existing.Date;
                break;

            case EditorMode.Transaction when query.TryGetValue("recordRecurringId", out var recordValue)
                && recordValue is int recordId && await database.GetAsync<RecurringTransaction>(recordId) is { } due:
                source = _recordFrom = due;
                Date = due.NextDate;
                IsRecording = true;
                break;
        }

        if (source is not null)
        {
            IsExisting = !IsRecording;
            Type = source.Type;
            AmountText = source.Amount.ToString("0.##");
            Note = source.Note;
        }
        else if (query.TryGetValue("type", out var typeValue) && typeValue is TransactionType type)
            Type = type;

        // A transfer has no category; keep expense categories ready in case the type is switched.
        await LoadCategoriesAsync(IsTransfer ? TransactionType.Expense : Type, source?.CategoryId ?? 0);
        await LoadAccountsAsync(
            source?.AccountId ?? Preferences.Default.Get(LastAccountKey, 0),
            source is not null && IsTransfer ? source.ToAccountId : Preferences.Default.Get(LastToAccountKey, 0));
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
        ITransactionFields target = Mode switch
        {
            EditorMode.Template => _template,
            EditorMode.Recurring => _recurring,
            _ => _transaction,
        };
        target.Amount = Math.Round(amount, 2);
        target.Type = Type;
        target.CategoryId = IsTransfer ? 0 : SelectedCategoryId;
        target.AccountId = SelectedAccountId;
        target.ToAccountId = IsTransfer ? SelectedToAccountId : 0;
        target.Note = Note.Trim();

        switch (Mode)
        {
            case EditorMode.Template:
                await database.SaveAsync(_template);
                break;

            case EditorMode.Recurring:
                await SaveRecurringAsync();
                break;

            default:
                _transaction.Date = Date.Date;
                await database.SaveTransactionAsync(_transaction);
                if (_recordFrom is not null)
                    await recurringService.AdvanceAsync(_recordFrom);
                break;
        }

        Preferences.Default.Set(LastAccountKey, SelectedAccountId);
        if (IsTransfer)
            Preferences.Default.Set(LastToAccountKey, SelectedToAccountId);
        await Shell.Current.GoToAsync("..");
    }

    async Task SaveRecurringAsync()
    {
        var frequency = SelectedFrequency;

        // Keep the original start (so a schedule on the 31st stays at month end) unless the schedule changed.
        if (!IsExisting || _recurring.NextDate.Date != Date.Date || _recurring.Frequency != frequency)
            _recurring.StartDate = _recurring.NextDate = Date.Date;
        _recurring.Frequency = frequency;
        _recurring.AutoAdd = AutoAdd;
        _recurring.RemindDaysBefore = Reminders.IsSupported ? int.Parse(Reminder.SelectedKey) : RecurringTransaction.NoReminder;

        if (_recurring.RemindDaysBefore != RecurringTransaction.NoReminder && !await Reminders.RequestPermissionAsync())
            await Shell.Current.DisplayAlertAsync("Notifications are off",
                "Allow notifications for Kwarta in Settings to get bill reminders.", "OK");

        await database.SaveAsync(_recurring);

        // Records it now if it is already due, and schedules its reminder.
        await recurringService.ProcessDueAsync();
    }

    [RelayCommand]
    async Task Delete()
    {
        if (!IsExisting)
            return;

        var what = Mode switch
        {
            EditorMode.Template => "quick-add",
            EditorMode.Recurring => "recurring transaction",
            _ => "transaction",
        };
        var detail = Mode == EditorMode.Recurring ? " Transactions it already added are kept." : string.Empty;
        if (!await Shell.Current.DisplayAlertAsync($"Delete {what}", $"Delete this {what}?{detail}", "Delete", "Cancel"))
            return;

        switch (Mode)
        {
            case EditorMode.Template:
                await database.DeleteAsync(_template);
                break;

            case EditorMode.Recurring:
                await database.DeleteAsync(_recurring);
                await recurringService.ScheduleRemindersAsync();
                break;

            default:
                await database.DeleteTransactionAsync(_transaction);
                break;
        }

        await Shell.Current.GoToAsync("..");
    }
}
