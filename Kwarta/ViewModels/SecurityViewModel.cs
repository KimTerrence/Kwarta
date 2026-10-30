using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Services;

namespace Kwarta.ViewModels;

public partial class SecurityViewModel(AppLock appLock) : ObservableObject
{
    bool _loading;

    [ObservableProperty]
    public partial bool IsLockEnabled { get; set; }

    [ObservableProperty]
    public partial bool AreBiometricsAvailable { get; set; }

    [ObservableProperty]
    public partial bool UseBiometrics { get; set; }

    public string BiometricsLabel => DeviceInfo.Platform == DevicePlatform.WinUI ? "Unlock with Windows Hello"
        : DeviceInfo.Platform == DevicePlatform.Android ? "Unlock with fingerprint or face"
        : "Unlock with Face ID or Touch ID";

    public async Task LoadAsync()
    {
        _loading = true;
        IsLockEnabled = appLock.IsEnabled;
        UseBiometrics = appLock.UseBiometrics;
        AreBiometricsAvailable = await AppLock.AreBiometricsAvailableAsync();
        _loading = false;
    }

    partial void OnUseBiometricsChanged(bool value)
    {
        if (!_loading)
            appLock.UseBiometrics = value;
    }

    [RelayCommand]
    async Task TurnOn()
    {
        if (await AskNewPinAsync() is not { } pin)
            return;
        appLock.SetPin(pin);
        await LoadAsync();
    }

    [RelayCommand]
    async Task ChangePin()
    {
        if (!await ConfirmCurrentPinAsync() || await AskNewPinAsync() is not { } pin)
            return;
        appLock.SetPin(pin);
        await Shell.Current.DisplayAlertAsync("App lock", "Your PIN was changed.", "OK");
    }

    [RelayCommand]
    async Task TurnOff()
    {
        if (!await ConfirmCurrentPinAsync())
            return;
        appLock.Disable();
        await LoadAsync();
    }

    async Task<bool> ConfirmCurrentPinAsync()
    {
        var pin = await PromptPinAsync("Enter your current PIN.");
        if (pin is null)
            return false;
        if (await Task.Run(() => appLock.CheckPin(pin)))
            return true;

        await Shell.Current.DisplayAlertAsync("App lock", "That PIN isn't right.", "OK");
        return false;
    }

    /// <summary>Asks for a new PIN twice. Returns null if cancelled or the two don't match.</summary>
    async Task<string?> AskNewPinAsync()
    {
        var pin = await PromptPinAsync($"Choose a {AppLock.MinPinLength}–{AppLock.MaxPinLength} digit PIN.");
        if (pin is null)
            return null;
        if (!AppLock.IsValidPin(pin))
        {
            await Shell.Current.DisplayAlertAsync("App lock",
                $"Use {AppLock.MinPinLength} to {AppLock.MaxPinLength} digits.", "OK");
            return null;
        }

        if (await PromptPinAsync("Enter the PIN again.") == pin)
            return pin;

        await Shell.Current.DisplayAlertAsync("App lock", "The PINs didn't match. Try again.", "OK");
        return null;
    }

    static Task<string?> PromptPinAsync(string message) =>
        Shell.Current.DisplayPromptAsync("App lock", message, "OK", "Cancel",
            maxLength: AppLock.MaxPinLength, keyboard: Keyboard.Numeric);
}
