using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kwarta.ViewModels;

public partial class PillOption(string key, string text) : ObservableObject
{
    public string Key { get; } = key;
    public string Text { get; } = text;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>A row of pills where one is selected, shown by <see cref="Views.PillGroupView"/>.</summary>
public partial class PillGroup : ObservableObject
{
    public PillGroup(params (string Key, string Text)[] options)
    {
        Options = options.Select(o => new PillOption(o.Key, o.Text)).ToList();
        SelectedKey = Options[0].Key;
    }

    public IReadOnlyList<PillOption> Options { get; }

    [ObservableProperty]
    public partial string SelectedKey { get; set; }

    partial void OnSelectedKeyChanged(string value)
    {
        foreach (var option in Options)
            option.IsSelected = option.Key == value;
    }

    [RelayCommand]
    void Select(PillOption option) => SelectedKey = option.Key;
}
