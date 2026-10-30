using System.Text;
using Kwarta.Models;
using Kwarta.Services;

namespace Kwarta.Tests;

public class BackupFileTests
{
    static Backup Sample() => new()
    {
        ExportedAt = new DateTime(2026, 10, 30, 14, 30, 0),
        Categories = [new Category { Id = 1, Name = "Food", MonthlyBudget = 8000, RolloverSince = new DateTime(2026, 9, 1) }],
        Accounts =
        [
            new Account { Id = 1, Name = "Cash", Type = AccountType.Cash },
            new Account { Id = 2, Name = "GCash", Type = AccountType.EWallet, StartingBalance = 500 },
        ],
        Transactions =
        [
            new Transaction { Id = 1, Amount = 150, CategoryId = 1, AccountId = 1, Note = "Lunch" },
            new Transaction { Id = 2, Amount = 1000, Type = TransactionType.Transfer, AccountId = 1, ToAccountId = 2 },
            new Transaction { Id = 3, Amount = -25.5m, Type = TransactionType.Adjustment, AccountId = 2 },
        ],
        RecurringTransactions = [new RecurringTransaction { Id = 1, Amount = 15000, CategoryId = 1, AccountId = 1 }],
        Templates = [new TransactionTemplate { Id = 1, Amount = 15, CategoryId = 1, AccountId = 1, Note = "Jeep fare" }],
        SavingsGoals = [new SavingsGoal { Id = 1, Name = "Emergency fund", TargetAmount = 50000, AccountId = 2 }],
        Debts = [new Debt { Id = 1, Person = "Juan", Amount = 500, PaidAmount = 200 }],
    };

    static Task<Backup> ReadAsync(string json) => BackupFile.ReadAsync(new MemoryStream(Encoding.UTF8.GetBytes(json)));

    [Fact]
    public async Task Round_trip_keeps_every_row_and_value()
    {
        var restored = await ReadAsync(BackupFile.Serialize(Sample()));

        Assert.Equal(Backup.CurrentVersion, restored.Version);
        Assert.Equal(new DateTime(2026, 9, 1), restored.Categories.Single().RolloverSince);
        Assert.Equal(500, restored.Accounts[1].StartingBalance);
        Assert.Equal(3, restored.Transactions.Count);
        Assert.Equal(-25.5m, restored.Transactions[2].Amount);
        Assert.Equal(TransactionType.Adjustment, restored.Transactions[2].Type);
        Assert.Equal("Jeep fare", restored.Templates.Single().Note);
        Assert.Equal(2, restored.SavingsGoals.Single().AccountId);
        Assert.Equal(300, restored.Debts.Single().Remaining);
    }

    [Fact]
    public void Enums_are_written_as_names_and_computed_properties_are_left_out()
    {
        var json = BackupFile.Serialize(Sample());

        Assert.Contains("\"Transfer\"", json);
        Assert.DoesNotContain("\"Glyph\"", json);
        Assert.DoesNotContain("\"Tint\"", json);
        Assert.DoesNotContain("\"Remaining\"", json);
    }

    [Fact]
    public async Task A_version_1_file_without_the_newer_lists_still_reads()
    {
        const string json = """
            { "Version": 1, "ExportedAt": "2026-10-01T09:00:00",
              "Categories": [ { "Id": 1, "Name": "Food" } ],
              "Accounts": [ { "Id": 1, "Name": "Cash" } ],
              "Transactions": [ { "Id": 1, "Amount": 50, "Type": "Expense", "CategoryId": 1, "AccountId": 1, "Note": "" } ] }
            """;

        var backup = await ReadAsync(json);

        Assert.Single(backup.Transactions);
        Assert.Empty(backup.RecurringTransactions);
        Assert.Empty(backup.Debts);
    }

    [Fact]
    public async Task Transactions_pointing_at_missing_categories_are_rejected()
    {
        var backup = Sample();
        backup.Transactions[0].CategoryId = 99;

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => ReadAsync(BackupFile.Serialize(backup)));
        Assert.Contains("transactions", error.Message);
    }

    [Fact]
    public async Task Goals_linked_to_missing_accounts_are_rejected()
    {
        var backup = Sample();
        backup.SavingsGoals[0].AccountId = 42;

        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAsync(BackupFile.Serialize(backup)));
    }

    [Fact]
    public async Task Duplicate_ids_are_rejected()
    {
        var backup = Sample();
        backup.Accounts[1].Id = 1;

        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAsync(BackupFile.Serialize(backup)));
    }

    [Fact]
    public async Task A_backup_from_a_newer_version_is_rejected()
    {
        var backup = Sample();
        backup.Version = Backup.CurrentVersion + 1;

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => ReadAsync(BackupFile.Serialize(backup)));
        Assert.Contains("newer version", error.Message);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("[]")]
    public async Task Files_that_are_not_backups_are_rejected(string content) =>
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAsync(content));
}
