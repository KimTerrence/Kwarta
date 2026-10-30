using Kwarta.Models;
using SQLite;

namespace Kwarta.Tests;

/// <summary>Checks the sqlite-net behavior BudgetDatabase.RestoreAsync relies on.</summary>
public class RestoreTests
{
    [Fact]
    public void Insert_or_replace_keeps_ids_on_auto_increment_tables()
    {
        using var db = new SQLiteConnection(":memory:");
        db.CreateTable<Account>();
        db.Insert(new Account { Name = "Will be replaced" });

        db.RunInTransaction(() =>
        {
            db.DeleteAll<Account>();
            db.InsertAll(new[] { new Account { Id = 7, Name = "GCash" }, new Account { Id = 3, Name = "BDO" } },
                "OR REPLACE", runInTransaction: false);
        });

        Assert.Equal([3, 7], db.Table<Account>().OrderBy(a => a.Id).ToList().Select(a => a.Id));

        // New rows continue after the highest restored id.
        var added = new Account { Name = "Cash" };
        db.Insert(added);
        Assert.Equal(8, added.Id);
    }

    [Fact]
    public void Nullable_rollover_date_round_trips()
    {
        using var db = new SQLiteConnection(":memory:");
        db.CreateTable<Category>();
        db.Insert(new Category { Name = "Food", RolloverSince = new DateTime(2026, 9, 1) });
        db.Insert(new Category { Name = "Rent" });

        var categories = db.Table<Category>().OrderBy(c => c.Id).ToList();
        Assert.Equal(new DateTime(2026, 9, 1), categories[0].RolloverSince);
        Assert.Null(categories[1].RolloverSince);
    }
}
