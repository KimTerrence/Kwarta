using System.Security.Cryptography;
using Plugin.Maui.Biometric;

namespace Kwarta.Services;

/// <summary>
/// PIN (and optionally fingerprint / face) lock shown when the app starts or returns after
/// <see cref="GracePeriod"/> in the background. Only a salted PBKDF2 hash of the PIN is stored.
/// </summary>
public class AppLock
{
    public const int MinPinLength = 4;
    public const int MaxPinLength = 6;

    static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(30);

    const string HashKey = "AppLockPinHash";
    const string SaltKey = "AppLockPinSalt";
    const string BiometricsKey = "AppLockBiometrics";
    const int Iterations = 100_000;

    DateTime? _backgroundedAt;

    public bool IsEnabled => Preferences.Default.ContainsKey(HashKey);

    /// <summary>Whether the lock screen is showing.</summary>
    public bool IsLocked { get; set; }

    public bool UseBiometrics
    {
        get => IsEnabled && Preferences.Default.Get(BiometricsKey, false);
        set => Preferences.Default.Set(BiometricsKey, value);
    }

    public static bool IsValidPin(string? pin) =>
        pin is { Length: >= MinPinLength and <= MaxPinLength } && pin.All(char.IsAsciiDigit);

    public void SetPin(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        Preferences.Default.Set(SaltKey, Convert.ToBase64String(salt));
        Preferences.Default.Set(HashKey, Convert.ToBase64String(Hash(pin, salt)));
    }

    public bool CheckPin(string pin)
    {
        var hash = Preferences.Default.Get<string?>(HashKey, null);
        var salt = Preferences.Default.Get<string?>(SaltKey, null);
        return hash is not null && salt is not null
            && CryptographicOperations.FixedTimeEquals(Hash(pin, Convert.FromBase64String(salt)), Convert.FromBase64String(hash));
    }

    public void Disable()
    {
        Preferences.Default.Remove(HashKey);
        Preferences.Default.Remove(SaltKey);
        Preferences.Default.Remove(BiometricsKey);
    }

    public void OnBackgrounded() => _backgroundedAt ??= DateTime.Now;

    /// <summary>Whether to lock now that the app is back in the foreground.</summary>
    public bool ShouldLockOnResume()
    {
        var away = _backgroundedAt is { } at ? DateTime.Now - at : TimeSpan.Zero;
        _backgroundedAt = null;
        return IsEnabled && !IsLocked && away >= GracePeriod;
    }

    public static async Task<bool> AreBiometricsAvailableAsync()
    {
        try
        {
            return await BiometricAuthenticationService.Default.GetAuthenticationStatusAsync() == BiometricHwStatus.Success;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static async Task<bool> AuthenticateWithBiometricsAsync()
    {
        try
        {
            var result = await BiometricAuthenticationService.Default.AuthenticateAsync(new AuthenticationRequest
            {
                Title = "Unlock Kwarta",
                NegativeText = "Use PIN",
            }, CancellationToken.None);
            return result.Status == BiometricResponseStatus.Success;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static byte[] Hash(string pin, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, 32);
}
