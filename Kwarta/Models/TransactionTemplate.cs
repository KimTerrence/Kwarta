using SQLite;

namespace Kwarta.Models;

/// <summary>A saved transaction, such as "Jeep fare ₱15 · Cash", that can be added again with one tap.</summary>
public class TransactionTemplate : IEntity, ITransactionFields
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public TransactionType Type { get; set; } = TransactionType.Expense;

    public int CategoryId { get; set; }

    public int AccountId { get; set; }

    public int ToAccountId { get; set; }

    public string Note { get; set; } = string.Empty;
}
