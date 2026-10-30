namespace Kwarta.Models;

public enum TransactionType
{
    Expense = 0,
    Income = 1,

    /// <summary>Money moved from <see cref="Transaction.AccountId"/> to <see cref="Transaction.ToAccountId"/>.</summary>
    Transfer = 2,

    /// <summary>
    /// Correction that brings <see cref="Transaction.AccountId"/> to its real balance. The amount is signed and
    /// isn't counted as income or spending.
    /// </summary>
    Adjustment = 3
}
