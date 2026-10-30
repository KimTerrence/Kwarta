using System.Globalization;
using System.Text;
using Kwarta.Models;

namespace Kwarta.Services;

public enum AutoBackupFrequency
{
    Off = 0,
    Daily = 1,
    Weekly = 2
}

/// <summary>Exports data to JSON or CSV, restores JSON backups, and runs the automatic backup.</summary>
public class BackupService(BudgetDatabase database)
{
    public const string AutoBackupFileName = "kwarta-auto-backup.json";

    /// <summary>Days without a backup before the Overview suggests one.</summary>
    public const int ReminderDays = 30;

    const string LastBackupKey = "LastBackupAt";
    const string FirstRunKey = "FirstRunAt";
    const string ReminderSnoozedKey = "BackupReminderSnoozedUntil";
    const string FrequencyKey = "AutoBackupFrequency";
    const string LastAutoBackupKey = "LastAutoBackupAt";
    const string AutoBackupErrorKey = "AutoBackupError";

    readonly SemaphoreSlim _autoBackupLock = new(1, 1);

    /// <summary>File types the import picker offers. Android apps often label JSON as plain text or binary.</summary>
    public static readonly FilePickerFileType FileType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.Android] = ["application/json", "text/plain", "application/octet-stream"],
        [DevicePlatform.iOS] = ["public.json"],
        [DevicePlatform.MacCatalyst] = ["public.json"],
        [DevicePlatform.WinUI] = [".json"],
    });

    /// <summary>When data last left the app, by export or automatic backup.</summary>
    public static DateTime? LastBackupAt => GetDate(LastBackupKey);

    public static DateTime? LastAutoBackupAt => GetDate(LastAutoBackupKey);

    public static string? AutoBackupError => Preferences.Default.Get<string?>(AutoBackupErrorKey, null);

    public static AutoBackupFrequency Frequency
    {
        get => (AutoBackupFrequency)Preferences.Default.Get(FrequencyKey, (int)AutoBackupFrequency.Off);
        set => Preferences.Default.Set(FrequencyKey, (int)value);
    }

    /// <summary>"Today", "Yesterday", "12 days ago", or "Never".</summary>
    public static string Describe(DateTime? date) => date is not { } d ? "Never"
        : (DateTime.Today - d.Date).Days switch
        {
            0 => "Today",
            1 => "Yesterday",
            var days => $"{days} days ago",
        };

    /// <summary>
    /// Whether to suggest a backup: none for <see cref="ReminderDays"/> days (counted from the first launch
    /// if there has never been one) and the reminder isn't snoozed.
    /// </summary>
    public static bool ShouldRemind()
    {
        if (GetDate(FirstRunKey) is null)
            SetDate(FirstRunKey, DateTime.Now);
        if (GetDate(ReminderSnoozedKey) is { } snoozedUntil && snoozedUntil > DateTime.Now)
            return false;

        var since = LastBackupAt ?? GetDate(FirstRunKey)!.Value;
        return (DateTime.Now - since).TotalDays >= ReminderDays;
    }

    public static void SnoozeReminder() => SetDate(ReminderSnoozedKey, DateTime.Now.AddDays(7));

    /// <summary>Exports a full backup. Returns false if the user cancelled the save dialog.</summary>
    public async Task<bool> ExportAsync()
    {
        var backup = await database.GetBackupAsync();
        var saved = await SaveFileAsync($"kwarta-backup-{backup.ExportedAt:yyyy-MM-dd-HHmm}.json",
            BackupFile.Serialize(backup), "application/json", "Kwarta backup");
        if (saved)
            SetDate(LastBackupKey, DateTime.Now);
        return saved;
    }

    /// <summary>Exports every transaction as CSV for spreadsheets. Returns false if the user cancelled.</summary>
    public async Task<bool> ExportCsvAsync()
    {
        var categories = (await database.GetCategoriesAsync()).ToDictionary(c => c.Id, c => c.Name);
        var accounts = (await database.GetAccountsAsync()).ToDictionary(a => a.Id, a => a.Name);

        var csv = new StringBuilder();
        csv.AppendLine(Csv.Row("Date", "Type", "Amount", "Category", "Account", "To account", "Note"));
        foreach (var t in await database.GetAllTransactionsAsync())
        {
            var signed = t.Type switch
            {
                TransactionType.Expense => -t.Amount,
                _ => t.Amount,
            };
            csv.AppendLine(Csv.Row(
                t.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                t.Type.ToString(),
                signed.ToString("0.00", CultureInfo.InvariantCulture),
                categories.GetValueOrDefault(t.CategoryId, string.Empty),
                accounts.GetValueOrDefault(t.AccountId, string.Empty),
                t.Type == TransactionType.Transfer ? accounts.GetValueOrDefault(t.ToAccountId, string.Empty) : string.Empty,
                t.Note));
        }

        return await SaveFileAsync($"kwarta-transactions-{DateTime.Now:yyyy-MM-dd}.csv", csv.ToString(), "text/csv",
            "CSV spreadsheet");
    }

    public Task RestoreAsync(Backup backup) => database.RestoreAsync(backup);

    /// <summary>Writes the automatic backup if it's on and due. Failures are kept in <see cref="AutoBackupError"/>.</summary>
    public async Task RunAutoBackupIfDueAsync(bool force = false)
    {
        var interval = Frequency switch
        {
            AutoBackupFrequency.Daily => TimeSpan.FromDays(1),
            AutoBackupFrequency.Weekly => TimeSpan.FromDays(7),
            _ => (TimeSpan?)null,
        };
        if (interval is null || !BackupFolder.IsReady)
            return;
        if (!force && LastAutoBackupAt is { } last && DateTime.Now - last < interval)
            return;

        if (!await _autoBackupLock.WaitAsync(0))
            return;
        try
        {
            await BackupFolder.WriteAsync(AutoBackupFileName, BackupFile.Serialize(await database.GetBackupAsync()));
            SetDate(LastAutoBackupKey, DateTime.Now);
            SetDate(LastBackupKey, DateTime.Now);
            Preferences.Default.Remove(AutoBackupErrorKey);
        }
        catch (Exception ex)
        {
            Preferences.Default.Set(AutoBackupErrorKey, ex.Message);
        }
        finally
        {
            _autoBackupLock.Release();
        }
    }

    /// <summary>Hands a file to the user: a save dialog on Windows, the share sheet elsewhere.</summary>
    static async Task<bool> SaveFileAsync(string fileName, string content, string mimeType, string typeLabel)
    {
#if WINDOWS
        var picker = WindowHandle.Initialize(new Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
            SuggestedFileName = Path.GetFileNameWithoutExtension(fileName),
        });
        picker.FileTypeChoices.Add(typeLabel, [Path.GetExtension(fileName)]);

        var file = await picker.PickSaveFileAsync();
        if (file is null)
            return false;
        await Windows.Storage.FileIO.WriteTextAsync(file, content);
        return true;
#else
        var path = Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllTextAsync(path, content);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = $"Save {typeLabel}",
            File = new ShareFile(path, mimeType),
        });
        return true;
#endif
    }

    static DateTime? GetDate(string key) =>
        Preferences.Default.ContainsKey(key) ? Preferences.Default.Get(key, DateTime.MinValue) : null;

    static void SetDate(string key, DateTime value) => Preferences.Default.Set(key, value);
}
