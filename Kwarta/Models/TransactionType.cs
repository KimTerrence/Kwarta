namespace Kwarta.Models;

public enum TransactionType
{
    Expense = 0,
    Income = 1,

    /// <summary>Money moved from <see cref="Transaction.AccountId"/> to <see cref="Transaction.ToAccountId"/>.</summary>
    Transfer = 2
}
