using SQLite;

namespace Kwarta.Models;

/// <summary>A transaction that repeats, such as rent, salary, a bill or a subscription.</summary>
public class RecurringTransaction : IEntity, ITransactionFields
{
    public const int NoReminder = -1;

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public TransactionType Type { get; set; } = TransactionType.Expense;

    public int CategoryId { get; set; }

    public int AccountId { get; set; }

    public int ToAccountId { get; set; }

    public string Note { get; set; } = string.Empty;

    public RecurringFrequency Frequency { get; set; } = RecurringFrequency.Monthly;

    /// <summary>First occurrence. Later ones are counted from it, so a schedule on the 31st stays at month end.</summary>
    public DateTime StartDate { get; set; } = DateTime.Today;

    /// <summary>Next occurrence that hasn't been recorded or skipped yet.</summary>
    [Indexed]
    public DateTime NextDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Record each occurrence automatically. When off (for bills whose amount changes), the occurrence
    /// waits on the Overview until it is recorded or skipped.
    /// </summary>
    public bool AutoAdd { get; set; } = true;

    /// <summary>Days before <see cref="NextDate"/> to send a notification, or <see cref="NoReminder"/>.</summary>
    public int RemindDaysBefore { get; set; } = NoReminder;
}
