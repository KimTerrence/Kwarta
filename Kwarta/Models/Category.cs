using System.Text.Json.Serialization;
using Kwarta.Services;
using SQLite;

namespace Kwarta.Models;

public class Category : IEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = string.Empty;

    /// <summary>Material icon name, a key of <see cref="Icons.Category"/>.</summary>
    public string Icon { get; set; } = Icons.DefaultCategoryIcon;

    public string ColorHex { get; set; } = Icons.CategoryColors[^1];

    public TransactionType Type { get; set; } = TransactionType.Expense;

    /// <summary>Monthly spending limit. Zero means no budget is set.</summary>
    public decimal MonthlyBudget { get; set; }

    /// <summary>
    /// First month whose unspent budget carries into the next, or null when rollover is off.
    /// See <see cref="BudgetMath.Carryover"/>.
    /// </summary>
    public DateTime? RolloverSince { get; set; }

    [Ignore, JsonIgnore]
    public string Glyph => Icons.CategoryGlyph(Icon);

    [Ignore, JsonIgnore]
    public Color Color => Color.FromArgb(ColorHex);

    /// <summary>Light tint of <see cref="Color"/> used behind the icon.</summary>
    [Ignore, JsonIgnore]
    public Color Tint => Color.WithAlpha(0.16f);

    public override string ToString() => Name;
}
