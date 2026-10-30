using Kwarta.Models;
using SQLite;

namespace Kwarta.Services;

public class BudgetDatabase
{
    const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

    static readonly string DatabasePath = Path.Combine(FileSystem.AppDataDirectory, "kwarta.db3");

    SQLiteAsyncConnection? _connection;

    async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        var connection = new SQLiteAsyncConnection(DatabasePath, Flags);
        await connection.CreateTableAsync<Category>();
        await connection.CreateTableAsync<Account>();
        await connection.CreateTableAsync<Transaction>();
        await connection.CreateTableAsync<RecurringTransaction>();
        await connection.CreateTableAsync<TransactionTemplate>();
        await connection.CreateTableAsync<SavingsGoal>();
        await connection.CreateTableAsync<Debt>();

        if (await connection.Table<Category>().CountAsync() == 0)
            await connection.InsertAllAsync(DefaultCategories());
        else
            await MigrateEmojiIconsAsync(connection);

        if (await connection.Table<Account>().CountAsync() == 0)
            await connection.InsertAllAsync(DefaultAccounts());
        await AssignUnassignedTransactionsAsync(connection);

        _connection = connection;
        return connection;
    }

    static IEnumerable<Account> DefaultAccounts() =>
    [
        new() { Name = "Cash", Type = AccountType.Cash, ColorHex = "#43A047" },
        new() { Name = "GCash", Type = AccountType.EWallet, ColorHex = "#1E88E5" },
        new() { Name = "MariBank", Type = AccountType.Bank, ColorHex = "#F57C00" },
        new() { Name = "BDO", Type = AccountType.Bank, ColorHex = "#3949AB" },
        new() { Name = "Security Bank", Type = AccountType.Bank, ColorHex = "#00897B" },
    ];

    /// <summary>Transactions recorded before accounts existed go to the first cash account.</summary>
    static async Task AssignUnassignedTransactionsAsync(SQLiteAsyncConnection connection)
    {
        var fallback = await connection.Table<Account>().Where(a => a.Type == AccountType.Cash).FirstOrDefaultAsync()
            ?? await connection.Table<Account>().FirstOrDefaultAsync();
        if (fallback is null)
            return;

        await connection.ExecuteAsync(
            "UPDATE \"Transaction\" SET AccountId = ? WHERE AccountId IS NULL OR AccountId = 0", fallback.Id);
    }

    static IEnumerable<Category> DefaultCategories() =>
    [
        new() { Name = "Food", Icon = "restaurant", ColorHex = "#F57C00", MonthlyBudget = 8000 },
        new() { Name = "Transport", Icon = "directions_bus", ColorHex = "#1E88E5", MonthlyBudget = 3000 },
        new() { Name = "Bills & Utilities", Icon = "lightbulb", ColorHex = "#F9A825", MonthlyBudget = 5000 },
        new() { Name = "Rent", Icon = "home", ColorHex = "#8E24AA" },
        new() { Name = "Shopping", Icon = "shopping_bag", ColorHex = "#D81B60", MonthlyBudget = 2000 },
        new() { Name = "Health", Icon = "medical_services", ColorHex = "#00897B" },
        new() { Name = "Entertainment", Icon = "movie", ColorHex = "#E53935", MonthlyBudget = 1500 },
        new() { Name = "Other", Icon = "category", ColorHex = "#607D8B" },
        new() { Name = "Salary", Icon = "work", ColorHex = "#43A047", Type = TransactionType.Income },
        new() { Name = "Side Hustle", Icon = "storefront", ColorHex = "#00ACC1", Type = TransactionType.Income },
        new() { Name = "Gifts", Icon = "card_giftcard", ColorHex = "#D81B60", Type = TransactionType.Income },
        new() { Name = "Other Income", Icon = "payments", ColorHex = "#3949AB", Type = TransactionType.Income },
    ];

    /// <summary>Earlier versions stored an emoji as the category icon; convert those to Material icon names.</summary>
    static async Task MigrateEmojiIconsAsync(SQLiteAsyncConnection connection)
    {
        var emojiToIcon = new Dictionary<string, (string Icon, string Color)>
        {
            ["🍔"] = ("restaurant", "#F57C00"),
            ["🚌"] = ("directions_bus", "#1E88E5"),
            ["💡"] = ("lightbulb", "#F9A825"),
            ["🏠"] = ("home", "#8E24AA"),
            ["🛍️"] = ("shopping_bag", "#D81B60"),
            ["💊"] = ("medical_services", "#00897B"),
            ["🎬"] = ("movie", "#E53935"),
            ["📦"] = ("category", "#607D8B"),
            ["💼"] = ("work", "#43A047"),
            ["🧰"] = ("storefront", "#00ACC1"),
            ["🎁"] = ("card_giftcard", "#D81B60"),
            ["💵"] = ("payments", "#3949AB"),
        };

        var categories = await connection.Table<Category>().ToListAsync();
        foreach (var category in categories.Where(c => !Icons.Category.ContainsKey(c.Icon ?? string.Empty)))
        {
            if (emojiToIcon.TryGetValue(category.Icon ?? string.Empty, out var mapped))
                (category.Icon, category.ColorHex) = mapped;
            else
            {
                category.Icon = category.Type == TransactionType.Income ? "payments" : Icons.DefaultCategoryIcon;
                category.ColorHex = Icons.CategoryColors[category.Id % Icons.CategoryColors.Count];
            }

            await connection.UpdateAsync(category);
        }
    }

    // Rows of any table

    public async Task<List<T>> GetAllAsync<T>() where T : IEntity, new()
    {
        var db = await GetConnectionAsync();
        return await db.Table<T>().ToListAsync();
    }

    public async Task<T?> GetAsync<T>(int id) where T : class, IEntity, new()
    {
        var db = await GetConnectionAsync();
        return await db.FindAsync<T>(id);
    }

    public async Task SaveAsync<T>(T item) where T : IEntity, new()
    {
        var db = await GetConnectionAsync();
        if (item.Id == 0)
            await db.InsertAsync(item);
        else
            await db.UpdateAsync(item);
    }

    public async Task DeleteAsync<T>(T item) where T : IEntity, new()
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync(item);
    }

    // Categories

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Category>().OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<List<Category>> GetCategoriesAsync(TransactionType type)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Category>().Where(c => c.Type == type).OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Category?> GetCategoryAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Category>().Where(c => c.Id == id).FirstOrDefaultAsync();
    }

    public async Task SaveCategoryAsync(Category category)
    {
        var db = await GetConnectionAsync();
        if (category.Id == 0)
            await db.InsertAsync(category);
        else
            await db.UpdateAsync(category);
    }

    /// <summary>Whether transactions, recurring transactions or templates use the category.</summary>
    public async Task<bool> IsCategoryInUseAsync(int categoryId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Transaction>().Where(t => t.CategoryId == categoryId).CountAsync() > 0
            || await db.Table<RecurringTransaction>().Where(r => r.CategoryId == categoryId).CountAsync() > 0
            || await db.Table<TransactionTemplate>().Where(t => t.CategoryId == categoryId).CountAsync() > 0;
    }

    public async Task DeleteCategoryAsync(Category category)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync(category);
    }

    // Accounts

    public async Task<List<Account>> GetAccountsAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Account>().OrderBy(a => a.Type).ThenBy(a => a.Name).ToListAsync();
    }

    public async Task<Account?> GetAccountAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Account>().Where(a => a.Id == id).FirstOrDefaultAsync();
    }

    public async Task SaveAccountAsync(Account account)
    {
        var db = await GetConnectionAsync();
        if (account.Id == 0)
            await db.InsertAsync(account);
        else
            await db.UpdateAsync(account);
    }

    /// <summary>Whether transactions, recurring transactions, templates or savings goals use the account.</summary>
    public async Task<bool> IsAccountInUseAsync(int accountId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Transaction>()
                .Where(t => t.AccountId == accountId || t.ToAccountId == accountId).CountAsync() > 0
            || await db.Table<RecurringTransaction>()
                .Where(r => r.AccountId == accountId || r.ToAccountId == accountId).CountAsync() > 0
            || await db.Table<TransactionTemplate>()
                .Where(t => t.AccountId == accountId || t.ToAccountId == accountId).CountAsync() > 0
            || await db.Table<SavingsGoal>().Where(g => g.AccountId == accountId).CountAsync() > 0;
    }

    public async Task DeleteAccountAsync(Account account)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync(account);
    }

    /// <summary>
    /// All-time net movement per account (excluding starting balances): income in, expenses out,
    /// transfers out of the source account and into the destination, adjustments by their signed amount.
    /// </summary>
    public async Task<Dictionary<int, decimal>> GetAccountNetTotalsAsync()
    {
        var db = await GetConnectionAsync();
        var rows = await db.QueryAsync<AccountTotal>(
            "SELECT AccountId, SUM(CASE WHEN Type IN (?, ?) THEN Amount ELSE -Amount END) AS Total " +
            "FROM \"Transaction\" GROUP BY AccountId " +
            "UNION ALL " +
            "SELECT ToAccountId AS AccountId, SUM(Amount) AS Total " +
            "FROM \"Transaction\" WHERE Type = ? GROUP BY ToAccountId",
            (int)TransactionType.Income, (int)TransactionType.Adjustment, (int)TransactionType.Transfer);
        return rows.GroupBy(r => r.AccountId).ToDictionary(g => g.Key, g => g.Sum(r => r.Total));
    }

    /// <summary>Current balance of every account, keyed by id.</summary>
    public async Task<Dictionary<int, decimal>> GetAccountBalancesAsync()
    {
        var netTotals = await GetAccountNetTotalsAsync();
        return (await GetAccountsAsync()).ToDictionary(a => a.Id, a => a.StartingBalance + netTotals.GetValueOrDefault(a.Id));
    }

    class AccountTotal
    {
        public int AccountId { get; set; }
        public decimal Total { get; set; }
    }

    // Transactions

    public async Task<Transaction?> GetTransactionAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Transaction>().Where(t => t.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<Transaction>> GetAllTransactionsAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Transaction>().OrderBy(t => t.Date).ThenBy(t => t.Id).ToListAsync();
    }

    public async Task<List<Transaction>> GetTransactionsAsync(DateTime from, DateTime toExclusive)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Transaction>()
            .Where(t => t.Date >= from && t.Date < toExclusive)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToListAsync();
    }

    /// <summary>Budget carried into <paramref name="month"/> for each category with rollover on.</summary>
    public async Task<Dictionary<int, decimal>> GetBudgetCarryoverAsync(IEnumerable<Category> categories, DateTime month)
    {
        var rollover = categories
            .Where(c => c.RolloverSince is not null && c.MonthlyBudget > 0 && c.RolloverSince < month)
            .ToList();
        if (rollover.Count == 0)
            return [];

        var since = BudgetMath.FirstOfMonth(rollover.Min(c => c.RolloverSince!.Value));
        var spent = (await GetTransactionsAsync(since, month))
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g
                .GroupBy(t => BudgetMath.FirstOfMonth(t.Date))
                .ToDictionary(m => m.Key, m => m.Sum(t => t.Amount)));

        return rollover.ToDictionary(c => c.Id, c => BudgetMath.Carryover(
            c.MonthlyBudget, c.RolloverSince!.Value, spent.GetValueOrDefault(c.Id) ?? [], month));
    }

    public async Task SaveTransactionAsync(Transaction transaction)
    {
        var db = await GetConnectionAsync();
        if (transaction.Id == 0)
            await db.InsertAsync(transaction);
        else
            await db.UpdateAsync(transaction);
    }

    public async Task DeleteTransactionAsync(Transaction transaction)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync(transaction);
    }

    // Backup

    public async Task<Backup> GetBackupAsync() => new()
    {
        ExportedAt = DateTime.Now,
        Categories = await GetCategoriesAsync(),
        Accounts = await GetAccountsAsync(),
        Transactions = await GetAllTransactionsAsync(),
        RecurringTransactions = await GetAllAsync<RecurringTransaction>(),
        Templates = await GetAllAsync<TransactionTemplate>(),
        SavingsGoals = await GetAllAsync<SavingsGoal>(),
        Debts = await GetAllAsync<Debt>(),
    };

    /// <summary>Deletes all data and inserts the backup's rows, keeping their ids so references between them hold.</summary>
    public async Task RestoreAsync(Backup backup)
    {
        var db = await GetConnectionAsync();
        await db.RunInTransactionAsync(conn =>
        {
            conn.DeleteAll<Transaction>();
            conn.DeleteAll<RecurringTransaction>();
            conn.DeleteAll<TransactionTemplate>();
            conn.DeleteAll<SavingsGoal>();
            conn.DeleteAll<Debt>();
            conn.DeleteAll<Account>();
            conn.DeleteAll<Category>();

            // "OR REPLACE" makes sqlite-net write the AutoIncrement Id instead of assigning a new one.
            conn.InsertAll(backup.Categories, "OR REPLACE", runInTransaction: false);
            conn.InsertAll(backup.Accounts, "OR REPLACE", runInTransaction: false);
            conn.InsertAll(backup.Transactions, "OR REPLACE", runInTransaction: false);
            conn.InsertAll(backup.RecurringTransactions, "OR REPLACE", runInTransaction: false);
            conn.InsertAll(backup.Templates, "OR REPLACE", runInTransaction: false);
            conn.InsertAll(backup.SavingsGoals, "OR REPLACE", runInTransaction: false);
            conn.InsertAll(backup.Debts, "OR REPLACE", runInTransaction: false);
        });
    }
}
