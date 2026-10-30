using Kwarta.Models;
using Kwarta.Services;

namespace Kwarta.ViewModels;

/// <summary>A transaction joined with its category and account(s), shaped for display.</summary>
public class TransactionItem(Transaction transaction, Category? category, Account? account, Account? toAccount = null)
{
    public Transaction Transaction { get; } = transaction;

    public bool IsIncome => Transaction.Type == TransactionType.Income;
    public bool IsExpense => Transaction.Type == TransactionType.Expense;
    public bool IsTransfer => Transaction.Type == TransactionType.Transfer;

    public string CategoryName => IsTransfer ? "Transfer" : category?.Name ?? "Uncategorized";
    public string Glyph => IsTransfer ? Icons.SwapHoriz : category?.Glyph ?? Icons.CategoryGlyph(null);
    public Color IconColor => IsTransfer ? AppColors.Transfer : category?.Color ?? AppColors.Muted;
    public Color IconTint => IconColor.WithAlpha(0.16f);
    public string AccountName => account?.Name ?? "No account";
    public string ToAccountName => toAccount?.Name ?? "No account";
    public string Note => Transaction.Note;

    /// <summary>
    /// Account(s), plus the note when there is one: "GCash · Lunch with team" or "Cash → BDO · Deposit".
    /// </summary>
    public string Subtitle
    {
        get
        {
            var accounts = IsTransfer ? $"{AccountName} → {ToAccountName}" : AccountName;
            return string.IsNullOrWhiteSpace(Transaction.Note) ? accounts : $"{accounts} · {Transaction.Note}";
        }
    }

    public string DateText => Transaction.Date.ToString("MMM d, yyyy");

    /// <summary>Effect on the overall balance. Transfers only move money between accounts, so they net to zero.</summary>
    public decimal SignedAmount => IsIncome ? Transaction.Amount : IsExpense ? -Transaction.Amount : 0;

    public string AmountText => (IsIncome ? "+" : IsExpense ? "-" : "") + Money.Format(Transaction.Amount);
    public Color AmountColor => IsIncome ? AppColors.Income : IsExpense ? AppColors.Expense : AppColors.Transfer;
}

/// <summary>Header row shown above the transactions of one day.</summary>
public class TransactionDayHeader(DateTime date, decimal net)
{
    public string Title { get; } = date.Date == DateTime.Today ? "Today"
        : date.Date == DateTime.Today.AddDays(-1) ? "Yesterday"
        : date.ToString("dddd, MMM d");

    public string TotalText { get; } = Money.Format(net);
}
