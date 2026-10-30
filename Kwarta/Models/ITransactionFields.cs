namespace Kwarta.Models;

/// <summary>
/// What a transaction records, shared by <see cref="Transaction"/> and the rows that produce one
/// (<see cref="RecurringTransaction"/>, <see cref="TransactionTemplate"/>) so one editor can fill them all.
/// </summary>
public interface ITransactionFields
{
    decimal Amount { get; set; }
    TransactionType Type { get; set; }
    int CategoryId { get; set; }
    int AccountId { get; set; }
    int ToAccountId { get; set; }
    string Note { get; set; }
}
