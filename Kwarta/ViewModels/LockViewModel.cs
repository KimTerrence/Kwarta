using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Services;

namespace Kwarta.ViewModels;

public partial class LockViewModel(AppLock appLock) : ObservableObject
{
    const int MaxAttempts = 5;
    static readonly TimeSpan Lockout = TimeSpan.FromSeconds(30);

    int _failedAttempts;
    DateTime _lockedOutUntil;
    bool _checking;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Dots))]
    public partial string Pin { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    public partial string Message { get; set; } = string.Empty;

    public string Dots => Pin.Length == 0 ? " " : string.Concat(Enumerable.Repeat("●  ", Pin.Length)).TrimEnd();
    public bool HasMessage => !string.IsNullOrEmpty(Message);
    public bool CanUseBiometrics => appLock.UseBiometrics;

    /// <summary>Raised once the PIN or biometrics check passes.</summary>
    public event EventHandler? Unlocked;

    [RelayCommand]
    async Task Digit(string digit)
    {
        if (_checking || Pin.Length >= AppLock.MaxPinLength)
            return;
        Message = string.Empty;
        Pin += digit;

        // Unlock as soon as a correct PIN is typed; only a full-length wrong PIN counts as a failed attempt.
        if (Pin.Length >= AppLock.MinPinLength)
            await CheckAsync(countFailure: Pin.Length == AppLock.MaxPinLength);
    }

    [RelayCommand]
    void Backspace()
    {
        if (Pin.Length > 0)
            Pin = Pin[..^1];
    }

    [RelayCommand]
    async Task Enter()
    {
        if (Pin.Length < AppLock.MinPinLength)
            Message = $"Enter at least {AppLock.MinPinLength} digits.";
        else
            await CheckAsync(countFailure: true);
    }

    [RelayCommand]
    public async Task UseBiometrics()
    {
        if (CanUseBiometrics && await AppLock.AuthenticateWithBiometricsAsync())
            Unlocked?.Invoke(this, EventArgs.Empty);
    }

    async Task CheckAsync(bool countFailure)
    {
        if (DateTime.Now < _lockedOutUntil)
        {
            Message = $"Too many tries. Wait {(int)Math.Ceiling((_lockedOutUntil - DateTime.Now).TotalSeconds)} seconds.";
            Pin = string.Empty;
            return;
        }

        _checking = true;
        var pin = Pin;
        var correct = await Task.Run(() => appLock.CheckPin(pin));
        _checking = false;

        if (correct)
        {
            _failedAttempts = 0;
            Unlocked?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (!countFailure)
            return;

        Pin = string.Empty;
        if (++_failedAttempts >= MaxAttempts)
        {
            _failedAttempts = 0;
            _lockedOutUntil = DateTime.Now + Lockout;
            Message = $"Too many tries. Wait {(int)Lockout.TotalSeconds} seconds.";
        }
        else
            Message = "Wrong PIN. Try again.";
    }
}
