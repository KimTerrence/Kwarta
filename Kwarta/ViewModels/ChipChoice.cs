using CommunityToolkit.Mvvm.ComponentModel;
using Kwarta.Models;

namespace Kwarta.ViewModels;

/// <summary>A category or account shown as a selectable chip in the transaction editor.</summary>
public partial class ChipChoice(int id, string name, string glyph, Color iconColor) : ObservableObject
{
    public ChipChoice(Category category) : this(category.Id, category.Name, category.Glyph, category.Color) { }

    public ChipChoice(Account account) : this(account.Id, account.Name, account.Glyph, account.Color) { }

    public int Id { get; } = id;
    public string Name { get; } = name;
    public string Glyph { get; } = glyph;
    public Color IconColor { get; } = iconColor;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
