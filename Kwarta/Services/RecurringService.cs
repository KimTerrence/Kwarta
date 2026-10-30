using Kwarta.Models;

namespace Kwarta.Services;

/// <summary>Records due recurring transactions and keeps their reminders scheduled.</summary>
public class RecurringService(BudgetDatabase database)
{
    /// <summary>Time of day reminders are sent.</summary>
    static readonly TimeSpan ReminderTime = TimeSpan.FromHours(9);

    readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Adds a transaction for every due occurrence of the auto-add recurring transactions, then refreshes
    /// reminders. Returns how many were added.
    /// </summary>
    public async Task<int> ProcessDueAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var added = 0;
            foreach (var recurring in await database.GetAllAsync<RecurringTransaction>())
            {
                if (!recurring.AutoAdd)
                    continue;

                var due = Recurrence.DueDates(recurring, DateTime.Today).ToList();
                if (due.Count == 0)
                    continue;

                foreach (var date in due)
                    await database.SaveTransactionAsync(ToTransaction(recurring, date));
                recurring.NextDate = Recurrence.NextAfter(recurring.StartDate, recurring.Frequency, due[^1]);
                await database.SaveAsync(recurring);
                added += due.Count;
            }

            await ScheduleRemindersAsync();
            return added;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Moves past the current occurrence after it was recorded by hand or skipped.</summary>
    public async Task AdvanceAsync(RecurringTransaction recurring)
    {
        recurring.NextDate = Recurrence.NextAfter(recurring.StartDate, recurring.Frequency, recurring.NextDate);
        await database.SaveAsync(recurring);
        await ScheduleRemindersAsync();
    }

    public async Task ScheduleRemindersAsync()
    {
        if (!Reminders.IsSupported)
            return;

        var categories = (await database.GetCategoriesAsync()).ToDictionary(c => c.Id);
        var accounts = (await database.GetAccountsAsync()).ToDictionary(a => a.Id);

        var reminders = new List<Reminder>();
        foreach (var r in await database.GetAllAsync<RecurringTransaction>())
        {
            if (r.RemindDaysBefore == RecurringTransaction.NoReminder)
                continue;

            var name = Name(r, categories.GetValueOrDefault(r.CategoryId), accounts.GetValueOrDefault(r.ToAccountId));
            var when = r.RemindDaysBefore switch
            {
                0 => "today",
                1 => "tomorrow",
                var days => $"in {days} days",
            };
            var account = accounts.GetValueOrDefault(r.AccountId)?.Name;
            reminders.Add(new Reminder(r.Id, r.NextDate.Date.AddDays(-r.RemindDaysBefore) + ReminderTime,
                $"{name} is due {when}",
                account is null ? Money.Format(r.Amount) : $"{Money.Format(r.Amount)} · {account}"));
        }

        Reminders.Schedule(reminders);
    }

    public static Transaction ToTransaction(ITransactionFields fields, DateTime date) => new()
    {
        Amount = fields.Amount,
        Type = fields.Type,
        CategoryId = fields.CategoryId,
        AccountId = fields.AccountId,
        ToAccountId = fields.ToAccountId,
        Note = fields.Note,
        Date = date.Date,
    };

    /// <summary>The note if there is one, otherwise the category (or "Transfer to BDO").</summary>
    public static string Name(ITransactionFields fields, Category? category, Account? toAccount) =>
        !string.IsNullOrWhiteSpace(fields.Note) ? fields.Note
        : fields.Type == TransactionType.Transfer ? $"Transfer to {toAccount?.Name ?? "account"}"
        : category?.Name ?? "Transaction";
}
