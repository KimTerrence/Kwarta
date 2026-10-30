using System.Text.Json.Serialization;
using Kwarta.Services;
using SQLite;

namespace Kwarta.Models;

/// <summary>An amount to save towards, such as an emergency fund.</summary>
public class SavingsGoal : IEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = string.Empty;

    public decimal TargetAmount { get; set; }

    /// <summary>
    /// Account the money is kept in; its balance is the amount saved. Zero means the goal isn't linked
    /// and <see cref="SavedAmount"/> is updated by hand.
    /// </summary>
    public int AccountId { get; set; }

    public decimal SavedAmount { get; set; }

    public string ColorHex { get; set; } = Icons.CategoryColors[7];

    [Ignore, JsonIgnore]
    public Color Color => Color.FromArgb(ColorHex);
}
