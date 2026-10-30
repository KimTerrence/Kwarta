using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Services;

namespace Kwarta.ViewModels;

public partial class BackupViewModel(BackupService backupService, RecurringService recurringService) : ObservableObject
{
    bool _loading;

    [ObservableProperty]
    public partial string LastBackupText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAutoBackupOn))]
    public partial bool IsAutoBackupReady { get; set; }

    [ObservableProperty]
    public partial string FolderText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AutoStatusText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Color AutoStatusColor { get; set; } = AppColors.Muted;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public PillGroup Frequency { get; } = new(
        (nameof(AutoBackupFrequency.Off), "Off"),
        (nameof(AutoBackupFrequency.Daily), "Daily"),
        (nameof(AutoBackupFrequency.Weekly), "Weekly"));

    public bool CanChooseFolder => BackupFolder.CanChoose;
    public bool IsAutoBackupOn => Frequency.SelectedKey != nameof(AutoBackupFrequency.Off);

    public string AutoBackupHint => BackupFolder.CanChoose
        ? $"Kwarta saves {BackupService.AutoBackupFileName} to the folder when you open the app. "
          + "Pick a folder that syncs, like Google Drive or OneDrive, to keep a copy off this device."
        : $"Kwarta saves {BackupService.AutoBackupFileName} to {BackupFolder.DisplayName} when you open the app. "
          + "It's included in your device backup.";

    public void Load()
    {
        _loading = true;
        Frequency.SelectedKey = BackupService.Frequency.ToString();
        _loading = false;
        Frequency.PropertyChanged -= OnFrequencyChanged;
        Frequency.PropertyChanged += OnFrequencyChanged;
        Refresh();
    }

    void Refresh()
    {
        LastBackupText = BackupService.LastBackupAt is { } last
            ? $"Last backup: {BackupService.Describe(last).ToLowerInvariant()} ({last:MMM d, h:mm tt})"
            : "You haven't backed up yet.";
        FolderText = BackupFolder.DisplayName;
        IsAutoBackupReady = BackupFolder.IsReady;
        OnPropertyChanged(nameof(IsAutoBackupOn));

        if (BackupService.AutoBackupError is { } error)
        {
            AutoStatusText = $"Last automatic backup failed: {error}";
            AutoStatusColor = AppColors.Expense;
        }
        else
        {
            AutoStatusText = $"Last automatic backup: {BackupService.Describe(BackupService.LastAutoBackupAt).ToLowerInvariant()}";
            AutoStatusColor = AppColors.Muted;
        }
    }

    async void OnFrequencyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_loading || e.PropertyName != nameof(PillGroup.SelectedKey))
            return;

        BackupService.Frequency = Enum.Parse<AutoBackupFrequency>(Frequency.SelectedKey);
        if (BackupService.Frequency != AutoBackupFrequency.Off && !BackupFolder.IsReady)
            await ChooseFolder();
        else
        {
            await backupService.RunAutoBackupIfDueAsync();
            Refresh();
        }
    }

    [RelayCommand]
    async Task ChooseFolder()
    {
        if (await BackupFolder.ChooseAsync())
            await backupService.RunAutoBackupIfDueAsync(force: true);
        Refresh();
    }

    [RelayCommand]
    async Task Export() => await RunAsync("Export failed", async () =>
    {
        if (await backupService.ExportAsync() && DeviceInfo.Platform == DevicePlatform.WinUI)
            await Shell.Current.DisplayAlertAsync("Backup saved", "Your data was exported.", "OK");
    });

    [RelayCommand]
    async Task ExportCsv() => await RunAsync("Export failed", async () =>
    {
        if (await backupService.ExportCsvAsync() && DeviceInfo.Platform == DevicePlatform.WinUI)
            await Shell.Current.DisplayAlertAsync("Spreadsheet saved", "Your transactions were exported.", "OK");
    });

    [RelayCommand]
    async Task Import()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Choose a Kwarta backup",
            FileTypes = BackupService.FileType,
        });
        if (file is null)
            return;

        Backup backup;
        try
        {
            await using var stream = await file.OpenReadAsync();
            backup = await BackupFile.ReadAsync(stream);
        }
        catch (InvalidDataException ex)
        {
            await Shell.Current.DisplayAlertAsync("Can't import", ex.Message, "OK");
            return;
        }

        var summary = $"{backup.Accounts.Count} accounts, {backup.Categories.Count} categories and " +
                      $"{backup.Transactions.Count} transactions from {backup.ExportedAt:MMM d, yyyy h:mm tt}";
        if (!await Shell.Current.DisplayAlertAsync("Replace all data?",
                $"This backup has {summary}. Everything currently in Kwarta will be replaced.", "Replace", "Cancel"))
            return;

        await RunAsync("Import failed", async () =>
        {
            await backupService.RestoreAsync(backup);
            await recurringService.ProcessDueAsync();
            await Shell.Current.DisplayAlertAsync("Backup restored", "Your data was imported.", "OK");
        });
    }

    async Task RunAsync(string failureTitle, Func<Task> action)
    {
        IsBusy = true;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(failureTitle, ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            Refresh();
        }
    }
}
