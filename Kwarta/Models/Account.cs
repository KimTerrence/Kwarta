using System.Text.Json.Serialization;
using Kwarta.Services;
using SQLite;

namespace Kwarta.Models;

/// <summary>Where money is kept: cash on hand, an e-wallet like GCash, or a bank account.</summary>
public class Account : IEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; } = AccountType.Cash;

    public string ColorHex { get; set; } = Icons.CategoryColors[^1];

    /// <summary>Balance before the first recorded transaction.</summary>
    public decimal StartingBalance { get; set; }

    [Ignore, JsonIgnore]
    public string Glyph => Icons.AccountGlyph(Type);

    [Ignore, JsonIgnore]
    public Color Color => Color.FromArgb(ColorHex);

    [Ignore, JsonIgnore]
    public Color Tint => Color.WithAlpha(0.16f);

    public override string ToString() => Name;
}
