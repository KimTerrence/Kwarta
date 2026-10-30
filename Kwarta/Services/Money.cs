using System.Globalization;

namespace Kwarta.Services;

public static class Money
{
    public const string Symbol = "₱";

    public static string Format(decimal amount) =>
        (amount < 0 ? "-" : string.Empty) + Symbol + Math.Abs(amount).ToString("N2", CultureInfo.InvariantCulture);

    public static bool TryParse(string? text, out decimal amount) =>
        decimal.TryParse(text?.Replace(Symbol, string.Empty).Replace(",", string.Empty).Trim(),
            NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
}
