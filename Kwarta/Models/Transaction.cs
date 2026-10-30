using SQLite;

namespace Kwarta.Models;

public class Transaction
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public TransactionType Type { get; set; } = TransactionType.Expense;

    [Indexed]
    public int CategoryId { get; set; }

    [Indexed]
    public int AccountId { get; set; }

    /// <summary>Destination account of a transfer; zero for income and expenses.</summary>
    [Indexed]
    public int ToAccountId { get; set; }

    [Indexed]
    public DateTime Date { get; set; } = DateTime.Today;

    public string Note { get; set; } = string.Empty;
}
