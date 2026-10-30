using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kwarta.Services;

public record Reminder(int Id, DateTime At, string Title, string Message);

[JsonSerializable(typeof(List<Reminder>))]
partial class ReminderJsonContext : JsonSerializerContext;

/// <summary>
/// Local notifications for upcoming bills. Android uses alarms (re-armed after a reboot), iOS and macOS the
/// system scheduler. Windows isn't supported: unpackaged apps can't schedule notifications.
/// </summary>
public static class Reminders
{
    /// <summary>iOS allows 64 pending notifications per app.</summary>
    const int MaxScheduled = 60;

    public static bool IsSupported => DeviceInfo.Platform == DevicePlatform.Android
        || DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst;

    public static async Task<bool> RequestPermissionAsync()
    {
#if ANDROID
        return await Permissions.RequestAsync<Permissions.PostNotifications>() == PermissionStatus.Granted;
#elif IOS || MACCATALYST
        var (granted, _) = await UserNotifications.UNUserNotificationCenter.Current.RequestAuthorizationAsync(
            UserNotifications.UNAuthorizationOptions.Alert | UserNotifications.UNAuthorizationOptions.Sound);
        return granted;
#else
        await Task.CompletedTask;
        return false;
#endif
    }

    /// <summary>Replaces every scheduled reminder with these.</summary>
    public static void Schedule(IEnumerable<Reminder> reminders)
    {
        var upcoming = reminders.Where(r => r.At > DateTime.Now).OrderBy(r => r.At).Take(MaxScheduled).ToList();
#if ANDROID
        Platforms.Android.ReminderAlarms.Replace(Platform.AppContext, upcoming);
#elif IOS || MACCATALYST
        var center = UserNotifications.UNUserNotificationCenter.Current;
        center.RemoveAllPendingNotificationRequests();
        foreach (var reminder in upcoming)
        {
            var content = new UserNotifications.UNMutableNotificationContent
            {
                Title = reminder.Title,
                Body = reminder.Message,
                Sound = UserNotifications.UNNotificationSound.Default,
            };
            var when = new Foundation.NSDateComponents
            {
                Year = reminder.At.Year, Month = reminder.At.Month, Day = reminder.At.Day,
                Hour = reminder.At.Hour, Minute = reminder.At.Minute,
            };
            center.AddNotificationRequest(UserNotifications.UNNotificationRequest.FromIdentifier(
                reminder.Id.ToString(), content,
                UserNotifications.UNCalendarNotificationTrigger.CreateTrigger(when, repeats: false)), null);
        }
#else
        _ = upcoming;
#endif
    }

    public static string Serialize(List<Reminder> reminders) =>
        JsonSerializer.Serialize(reminders, ReminderJsonContext.Default.ListReminder);

    public static List<Reminder> Deserialize(string? json) =>
        string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize(json, ReminderJsonContext.Default.ListReminder) ?? [];
}
