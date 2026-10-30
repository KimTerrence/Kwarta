using Kwarta.Models;
using Kwarta.Services;

namespace Kwarta.ViewModels;

/// <summary>A recurring transaction joined with its category and account(s), shaped for display.</summary>
public class RecurringItem(RecurringTransaction recurring, Category? category, Account? account, Account? toAccount)
{
    public RecurringTransaction Recurring { get; } = recurring;

    bool IsTransfer => Recurring.Type == TransactionType.Transfer;

    public string Name => RecurringService.Name(Recurring, category, toAccount);
    public string Glyph => IsTransfer ? Icons.SwapHoriz : category?.Glyph ?? Icons.CategoryGlyph(null);
    public Color IconColor => IsTransfer ? AppColors.Transfer : category?.Color ?? AppColors.Muted;
    public Color IconTint => IconColor.WithAlpha(0.16f);

    public string AmountText => Recurring.Type switch
    {
        TransactionType.Income => "+",
        TransactionType.Expense => "-",
        _ => string.Empty,
    } + Money.Format(Recurring.Amount);

    public Color AmountColor => Recurring.Type switch
    {
        TransactionType.Income => AppColors.Income,
        TransactionType.Expense => AppColors.Expense,
        _ => AppColors.Transfer,
    };

    public string DetailText
    {
        get
        {
            var accounts = IsTransfer ? $"{account?.Name} → {toAccount?.Name}" : account?.Name;
            var mode = Recurring.AutoAdd ? "added automatically" : "recorded by you";
            return $"{Recurrence.Describe(Recurring.Frequency, Recurring.StartDate)} · {accounts} · {mode}";
        }
    }

    /// <summary>Due and waiting to be recorded by hand (auto-add ones are recorded on their own).</summary>
    public bool NeedsRecording => !Recurring.AutoAdd && Recurring.NextDate.Date <= DateTime.Today;

    public string DueText
    {
        get
        {
            var next = Recurring.NextDate.Date;
            if (NeedsRecording)
                return next == DateTime.Today ? "Due today" : $"Overdue since {next:MMM d}";
            return (next - DateTime.Today).Days switch
            {
                0 => "Today",
                1 => "Tomorrow",
                < 7 and var days => $"{next:dddd} · in {days} days",
                _ => $"Next on {next:MMM d, yyyy}",
            };
        }
    }

    public Color DueColor => NeedsRecording ? AppColors.Expense : AppColors.Muted;
}

/// <summary>A quick-add template shaped for display.</summary>
public class TemplateItem(TransactionTemplate template, Category? category, Account? account, Account? toAccount)
{
    public TransactionTemplate Template { get; } = template;

    public string Name => RecurringService.Name(Template, category, toAccount);
    public string Glyph => Template.Type == TransactionType.Transfer ? Icons.SwapHoriz : category?.Glyph ?? Icons.CategoryGlyph(null);
    public Color IconColor => Template.Type == TransactionType.Transfer ? AppColors.Transfer : category?.Color ?? AppColors.Muted;
    public Color IconTint => IconColor.WithAlpha(0.16f);
    public string AmountText => Money.Format(Template.Amount);

    public string AccountText => Template.Type == TransactionType.Transfer
        ? $"{account?.Name} → {toAccount?.Name}"
        : account?.Name ?? "No account";
}
