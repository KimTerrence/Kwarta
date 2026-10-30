namespace Kwarta.Services;

/// <summary>
/// Where automatic backups are written: a folder the user picks on Android (a document-tree URI) and
/// Windows (a path), or the app's Documents folder on iOS and macOS, which the Files app shows.
/// </summary>
public static class BackupFolder
{
    const string LocationKey = "AutoBackupFolder";

    static string? Location
    {
        get => Preferences.Default.Get<string?>(LocationKey, null);
        set => Preferences.Default.Set(LocationKey, value);
    }

    public static bool CanChoose => DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.WinUI;

    /// <summary>Whether a backup can be written now: a folder is chosen, or there is nothing to choose.</summary>
    public static bool IsReady => !CanChoose || Location is not null;

    public static string DisplayName
    {
        get
        {
            if (!CanChoose)
                return DeviceInfo.Platform == DevicePlatform.iOS ? "Files › On My iPhone › Kwarta" : "Kwarta's Documents folder";
            if (Location is not { } location)
                return "Not chosen";
#if ANDROID
            return AndroidX.DocumentFile.Provider.DocumentFile.FromTreeUri(
                Platform.AppContext, Android.Net.Uri.Parse(location)!)?.Name ?? location;
#else
            return location;
#endif
        }
    }

    /// <summary>Asks the user for a folder. Returns false if they cancelled.</summary>
    public static async Task<bool> ChooseAsync()
    {
#if ANDROID
        var intent = new Android.Content.Intent(Android.Content.Intent.ActionOpenDocumentTree);
        intent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission
            | Android.Content.ActivityFlags.GrantWriteUriPermission
            | Android.Content.ActivityFlags.GrantPersistableUriPermission);
        if ((await MainActivity.StartForResultAsync(intent))?.Data is not { } uri)
            return false;

        // Keep access after the app restarts.
        Platform.AppContext.ContentResolver!.TakePersistableUriPermission(uri,
            Android.Content.ActivityFlags.GrantReadUriPermission | Android.Content.ActivityFlags.GrantWriteUriPermission);
        Location = uri.ToString();
        return true;
#elif WINDOWS
        var picker = WindowHandle.Initialize(new Windows.Storage.Pickers.FolderPicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
        });
        picker.FileTypeFilter.Add("*");
        if (await picker.PickSingleFolderAsync() is not { } folder)
            return false;
        Location = folder.Path;
        return true;
#else
        await Task.CompletedTask;
        return true;
#endif
    }

    /// <summary>Writes (or overwrites) a file in the folder.</summary>
    public static async Task WriteAsync(string fileName, string content)
    {
#if ANDROID
        var context = Platform.AppContext;
        var tree = Location is { } location
            ? AndroidX.DocumentFile.Provider.DocumentFile.FromTreeUri(context, Android.Net.Uri.Parse(location)!)
            : null;
        if (tree is null || !tree.CanWrite())
            throw new IOException("The backup folder can't be reached. Choose it again.");

        var file = tree.FindFile(fileName) ?? tree.CreateFile("application/json", fileName)
            ?? throw new IOException("Couldn't create the backup file.");
        await using var stream = context.ContentResolver!.OpenOutputStream(file.Uri!, "wt")
            ?? throw new IOException("Couldn't open the backup file.");
        await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(content));
#elif WINDOWS
        if (Location is not { } location || !Directory.Exists(location))
            throw new IOException("The backup folder can't be reached. Choose it again.");
        await File.WriteAllTextAsync(Path.Combine(location, fileName), content);
#else
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        Directory.CreateDirectory(documents);
        await File.WriteAllTextAsync(Path.Combine(documents, fileName), content);
#endif
    }
}
