namespace Kwarta.ViewModels;

public static class AppColors
{
    public static readonly Color Income = Color.FromArgb("#1E9E5A");
    public static readonly Color Expense = Color.FromArgb("#D64545");
    public static readonly Color Transfer = Color.FromArgb("#1E88E5");
    public static readonly Color Warning = Color.FromArgb("#E6A23C");
    public static readonly Color Muted = Color.FromArgb("#8A8A8A");

    /// <summary>Green while under 80% of budget, amber near the limit, red when over.</summary>
    public static Color ForProgress(double progress, bool isOver) =>
        isOver ? Expense : progress >= 0.8 ? Warning : Income;
}
