using CommunityToolkit.Mvvm.ComponentModel;

namespace Kwarta.ViewModels;

/// <summary>One entry in a single-select grid (an icon or a color swatch).</summary>
public partial class SelectableOption(string key, string glyph, Color color) : ObservableObject
{
    public string Key { get; } = key;
    public string Glyph { get; } = glyph;
    public Color Color { get; } = color;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
