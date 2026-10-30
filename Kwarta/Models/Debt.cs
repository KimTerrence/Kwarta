using System.Text.Json.Serialization;
using SQLite;

namespace Kwarta.Models;

/// <summary>Money lent or borrowed (utang). Kept apart from accounts: it doesn't change any balance.</summary>
public class Debt : IEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Person { get; set; } = string.Empty;

    public DebtDirection Direction { get; set; } = DebtDirection.Lent;

    public decimal Amount { get; set; }

    public decimal PaidAmount { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    public string Note { get; set; } = string.Empty;

    [Ignore, JsonIgnore]
    public decimal Remaining => Math.Max(0, Amount - PaidAmount);

    [Ignore, JsonIgnore]
    public bool IsSettled => PaidAmount >= Amount;
}
