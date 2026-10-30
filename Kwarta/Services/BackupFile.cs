using System.Text.Json;
using System.Text.Json.Serialization;
using Kwarta.Models;

namespace Kwarta.Services;

/// <summary>Contents of a backup file: everything stored in the app.</summary>
public class Backup
{
    /// <summary>Version 2 added recurring transactions, templates, savings goals and debts.</summary>
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;
    public DateTime ExportedAt { get; set; }
    public List<Category> Categories { get; set; } = [];
    public List<Account> Accounts { get; set; } = [];
    public List<Transaction> Transactions { get; set; } = [];
    public List<RecurringTransaction> RecurringTransactions { get; set; } = [];
    public List<TransactionTemplate> Templates { get; set; } = [];
    public List<SavingsGoal> SavingsGoals { get; set; } = [];
    public List<Debt> Debts { get; set; } = [];
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Backup))]
partial class BackupJsonContext : JsonSerializerContext;

public static class BackupFile
{
    public static string Serialize(Backup backup) => JsonSerializer.Serialize(backup, BackupJsonContext.Default.Backup);

    /// <summary>Reads and checks a backup file. Throws <see cref="InvalidDataException"/> if it can't be restored.</summary>
    public static async Task<Backup> ReadAsync(Stream stream)
    {
        Backup? backup;
        try
        {
            backup = await JsonSerializer.DeserializeAsync(stream, BackupJsonContext.Default.Backup);
        }
        catch (JsonException)
        {
            throw new InvalidDataException("This file isn't a Kwarta backup.");
        }

        if (backup is null || backup.Categories is null || backup.Accounts is null || backup.Transactions is null)
            throw new InvalidDataException("This file isn't a Kwarta backup.");
        if (backup.Version > Backup.CurrentVersion)
            throw new InvalidDataException("This backup was made by a newer version of Kwarta. Update the app and try again.");
        if (backup.Accounts.Count == 0)
            throw new InvalidDataException("This backup has no accounts.");

        // Lists added in later versions are missing from older files.
        backup.RecurringTransactions ??= [];
        backup.Templates ??= [];
        backup.SavingsGoals ??= [];
        backup.Debts ??= [];

        var categoryIds = UniqueIds(backup.Categories, c => c.Id, c => c.Name, "categories");
        var accountIds = UniqueIds(backup.Accounts, a => a.Id, a => a.Name, "accounts");
        UniqueIds(backup.SavingsGoals, g => g.Id, g => g.Name, "savings goals");
        UniqueIds(backup.Debts, d => d.Id, d => d.Person, "debts");

        CheckReferences(backup.Transactions, t => t.Id, "transactions", categoryIds, accountIds);
        CheckReferences(backup.RecurringTransactions, r => r.Id, "recurring transactions", categoryIds, accountIds);
        CheckReferences(backup.Templates, t => t.Id, "quick-add templates", categoryIds, accountIds);

        if (backup.SavingsGoals.Any(g => g.AccountId != 0 && !accountIds.Contains(g.AccountId)))
            throw new InvalidDataException("This backup has savings goals linked to missing accounts.");

        foreach (var debt in backup.Debts)
            debt.Note ??= string.Empty;

        return backup;
    }

    static HashSet<int> UniqueIds<T>(List<T> items, Func<T, int> id, Func<T, string?> name, string what)
    {
        var ids = new HashSet<int>();
        if (items.Any(i => i is null || id(i) <= 0 || !ids.Add(id(i)) || string.IsNullOrWhiteSpace(name(i))))
            throw new InvalidDataException($"This backup has invalid {what}.");
        return ids;
    }

    static void CheckReferences<T>(List<T> items, Func<T, int> id, string what,
        HashSet<int> categoryIds, HashSet<int> accountIds) where T : ITransactionFields
    {
        var ids = new HashSet<int>();
        foreach (var item in items)
        {
            var valid = item is not null && id(item) > 0 && ids.Add(id(item)) && accountIds.Contains(item.AccountId)
                && item.Type switch
                {
                    TransactionType.Transfer => accountIds.Contains(item.ToAccountId),
                    TransactionType.Adjustment => true,
                    _ => categoryIds.Contains(item.CategoryId),
                };
            if (!valid)
                throw new InvalidDataException($"This backup has {what} that refer to missing accounts or categories.");
            item!.Note ??= string.Empty;
        }
    }
}
