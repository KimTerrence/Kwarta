using Microsoft.Maui.Controls.Shapes;

namespace Kwarta.Views;

/// <summary>
/// Rounded progress meter. The track is a light tint of the fill color, so the bar's
/// state (on track, near limit, over) reads across its whole width.
/// Built from plain layout views rather than GraphicsView, which crashes on Windows
/// when Shell tabs detach and re-attach it.
/// </summary>
public class MeterBar : ContentView
{
    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(double), typeof(MeterBar), 0d,
            propertyChanged: (b, _, _) => ((MeterBar)b).UpdateFill());

    public static readonly BindableProperty BarColorProperty =
        BindableProperty.Create(nameof(BarColor), typeof(Color), typeof(MeterBar), Colors.Green,
            propertyChanged: (b, _, _) => ((MeterBar)b).UpdateColors());

    readonly Border _track = new() { StrokeThickness = 0 };
    readonly Border _fill = new() { StrokeThickness = 0, HorizontalOptions = LayoutOptions.Start };

    public MeterBar()
    {
        HeightRequest = 8;
        Content = new Grid { Children = { _track, _fill } };
        SizeChanged += (_, _) => UpdateFill();
        UpdateColors();
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public Color BarColor
    {
        get => (Color)GetValue(BarColorProperty);
        set => SetValue(BarColorProperty, value);
    }

    void UpdateColors()
    {
        _track.BackgroundColor = BarColor.WithAlpha(0.18f);
        _fill.BackgroundColor = BarColor;
    }

    void UpdateFill()
    {
        if (Width <= 0 || Height <= 0)
            return;

        var shape = new RoundRectangle { CornerRadius = Height / 2 };
        _track.StrokeShape = shape;
        _fill.StrokeShape = new RoundRectangle { CornerRadius = Height / 2 };

        var progress = Math.Clamp(Progress, 0, 1);
        _fill.IsVisible = progress > 0;
        _fill.WidthRequest = Math.Max(Width * progress, Height);
    }
}
