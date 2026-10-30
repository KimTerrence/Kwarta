using Kwarta.Services;
using Kwarta.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Kwarta
{
    public partial class App : Application
    {
        readonly IServiceProvider _services;
        readonly AppLock _appLock;
        readonly BackupService _backupService;
        readonly RecurringService _recurringService;
        bool _started;

        public App(IServiceProvider services, AppLock appLock, BackupService backupService, RecurringService recurringService)
        {
            InitializeComponent();
            _services = services;
            _appLock = appLock;
            _backupService = backupService;
            _recurringService = recurringService;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();
            var window = new Window(shell);

            shell.Loaded += async (_, _) =>
            {
                if (_started)
                    return;
                _started = true;

                // Lock straight away: the launch animation ends by revealing the Overview, which must stay
                // hidden until the PIN is entered. The animation then plays unseen behind the lock screen.
                if (_appLock.IsEnabled)
                    await ShowLockAsync();
                await SplashOverlay.Finished;
                await RunUpkeepAsync();
            };
            window.Stopped += (_, _) => _appLock.OnBackgrounded();
            window.Resumed += async (_, _) =>
            {
                // Still animating: the shell's Loaded handler locks and runs upkeep afterwards.
                if (!SplashOverlay.HasPlayed)
                    return;
                if (_appLock.ShouldLockOnResume())
                    await ShowLockAsync();
                await RunUpkeepAsync();
            };

            return window;
        }

        async Task ShowLockAsync()
        {
            if (_appLock.IsLocked)
                return;
            _appLock.IsLocked = true;

            var lockPage = _services.GetRequiredService<LockPage>();
            lockPage.Unlocked += async (_, _) =>
            {
                // PIN and biometrics can both succeed; only dismiss once.
                if (!_appLock.IsLocked)
                    return;
                _appLock.IsLocked = false;
                await Shell.Current.Navigation.PopModalAsync(false);
            };
            await Shell.Current.Navigation.PushModalAsync(lockPage, false);
        }

        /// <summary>Records due recurring transactions and writes the automatic backup when it's due.</summary>
        async Task RunUpkeepAsync()
        {
            try
            {
                await _recurringService.ProcessDueAsync();
                await _backupService.RunAutoBackupIfDueAsync();
            }
            catch (Exception ex)
            {
                // Upkeep runs again on the next start or resume; don't take the app down over it.
                System.Diagnostics.Debug.WriteLine($"Upkeep failed: {ex}");
            }
        }
    }
}
