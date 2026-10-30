using Android.App;
using Android.Content;
using AndroidX.Core.App;
using Kwarta.Services;

namespace Kwarta.Platforms.Android;

/// <summary>
/// Schedules reminders as alarms. The list is kept in shared preferences so alarms can be cancelled later
/// and re-armed after a reboot, which clears them.
/// </summary>
static class ReminderAlarms
{
    const string PrefsName = "kwarta_reminders";
    const string ScheduledKey = "scheduled";
    const string ChannelId = "bills";

    public static void Replace(Context context, List<Reminder> reminders)
    {
        var alarms = (AlarmManager)context.GetSystemService(Context.AlarmService)!;
        var prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;

        foreach (var old in Reminders.Deserialize(prefs.GetString(ScheduledKey, null)))
            alarms.Cancel(PendingIntentFor(context, old));

        Arm(context, alarms, reminders);
        prefs.Edit()!.PutString(ScheduledKey, Reminders.Serialize(reminders))!.Apply();
    }

    public static void Rearm(Context context)
    {
        var prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;
        var reminders = Reminders.Deserialize(prefs.GetString(ScheduledKey, null)).Where(r => r.At > DateTime.Now);
        Arm(context, (AlarmManager)context.GetSystemService(Context.AlarmService)!, reminders);
    }

    static void Arm(Context context, AlarmManager alarms, IEnumerable<Reminder> reminders)
    {
        foreach (var reminder in reminders)
        {
            var at = new DateTimeOffset(reminder.At).ToUnixTimeMilliseconds();
            // Inexact alarms don't need the exact-alarm permission; a few minutes' delay is fine for a bill.
            alarms.SetAndAllowWhileIdle(AlarmType.RtcWakeup, at, PendingIntentFor(context, reminder));
        }
    }

    static PendingIntent PendingIntentFor(Context context, Reminder reminder)
    {
        var intent = new Intent(context, typeof(ReminderReceiver))
            .PutExtra("title", reminder.Title)
            .PutExtra("message", reminder.Message)
            .PutExtra("id", reminder.Id);
        return PendingIntent.GetBroadcast(context, reminder.Id, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    }

    public static void Show(Context context, int id, string title, string message)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
            manager.CreateNotificationChannel(new NotificationChannel(ChannelId, "Bill reminders", NotificationImportance.Default));
        }

        var launch = context.PackageManager!.GetLaunchIntentForPackage(context.PackageName!);
        var open = launch is null ? null : PendingIntent.GetActivity(context, 0, launch, PendingIntentFlags.Immutable);
        var notification = new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(Resource.Mipmap.appicon)!
            .SetContentTitle(title)!
            .SetContentText(message)!
            .SetContentIntent(open)!
            .SetAutoCancel(true)!
            .Build()!;

        try
        {
            NotificationManagerCompat.From(context)!.Notify(id, notification);
        }
        catch (Java.Lang.SecurityException)
        {
            // Notification permission was revoked.
        }
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public class ReminderReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null)
            return;
        ReminderAlarms.Show(context, intent.GetIntExtra("id", 0),
            intent.GetStringExtra("title") ?? "Kwarta", intent.GetStringExtra("message") ?? string.Empty);
    }
}

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted])]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is not null && intent?.Action == Intent.ActionBootCompleted)
            ReminderAlarms.Rearm(context);
    }
}
