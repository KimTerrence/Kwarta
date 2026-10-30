using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Kwarta
{
    [Activity(Theme = "@style/Kwarta.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        const int PickRequestCode = 4201;

        static TaskCompletionSource<Intent?>? _pending;

        /// <summary>Starts a system picker (such as the folder picker) and returns its result, or null if cancelled.</summary>
        public static Task<Intent?> StartForResultAsync(Intent intent)
        {
            _pending?.TrySetResult(null);
            _pending = new TaskCompletionSource<Intent?>();
            Platform.CurrentActivity!.StartActivityForResult(intent, PickRequestCode);
            return _pending.Task;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == PickRequestCode)
                _pending?.TrySetResult(resultCode == Result.Ok ? data : null);
        }
    }
}
