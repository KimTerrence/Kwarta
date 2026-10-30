using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Models;
using Kwarta.Services;
using Kwarta.Views;

namespace Kwarta.ViewModels;

public class MoreRow(string glyph, Color color, string title, string subtitle, string route)
{
    public string Glyph { get; } = glyph;
    public Color Color { get; } = color;
    public Color Tint { get; } = color.WithAlpha(0.16f);
    public string Title { get; } = title;
    public string Subtitle { get; } = subtitle;
    public string Route { get; } = route;
}

public class MoreSection(string title, IReadOnlyList<MoreRow> rows)
{
    public string Title { get; } = title;
    public IReadOnlyList<MoreRow> Rows { get; } = rows;
}

public partial class MoreViewModel(BudgetDatabase database, AppLock appLock) : ObservableObject
{
    public ObservableCollection<MoreSection> Sections { get; } = [];

    public async Task LoadAsync()
    {
        var recurring = await database.GetAllAsync<RecurringTransaction>();
        var goals = await database.GetAllAsync<SavingsGoal>();
        var templates = await database.GetAllAsync<TransactionTemplate>();
        var debts = (await database.GetAllAsync<Debt>()).Where(d => !d.IsSettled).ToList();
        var owedToYou = debts.Where(d => d.Direction == DebtDirection.Lent).Sum(d => d.Remaining);
        var youOwe = debts.Where(d => d.Direction == DebtDirection.Borrowed).Sum(d => d.Remaining);

        Sections.Clear();
        Sections.Add(new MoreSection("Insights",
        [
            new(Icons.BarChart, Color.FromArgb("#1E88E5"), "Reports", "Spending trends and where your money goes",
                nameof(ReportsPage)),
        ]));
        Sections.Add(new MoreSection("Planning",
        [
            new(Icons.EventRepeat, Color.FromArgb("#8E24AA"), "Recurring & bills",
                recurring.Count == 0 ? "Rent, salary, bills and subscriptions" : Count(recurring.Count, "scheduled", "scheduled"),
                nameof(RecurringPage)),
            new(Icons.Savings, Color.FromArgb("#43A047"), "Savings goals",
                goals.Count == 0 ? "Save towards an emergency fund or a trip" : Count(goals.Count, "goal", "goals"),
                nameof(GoalsPage)),
            new(Icons.Handshake, Color.FromArgb("#F57C00"), "Utang",
                debts.Count == 0 ? "Money you lent or borrowed"
                    : $"{Money.Format(owedToYou)} owed to you · {Money.Format(youOwe)} you owe",
                nameof(DebtsPage)),
            new(Icons.Bolt, Color.FromArgb("#F9A825"), "Quick add",
                templates.Count == 0 ? "Save common entries to add with one tap" : Count(templates.Count, "saved", "saved"),
                nameof(TemplatesPage)),
        ]));
        Sections.Add(new MoreSection("Settings",
        [
            new(Icons.Backup, Color.FromArgb("#00897B"), "Backup & export",
                $"Last backup: {BackupService.Describe(BackupService.LastBackupAt).ToLowerInvariant()}", nameof(BackupPage)),
            new(Icons.Lock, Color.FromArgb("#3949AB"), "App lock", appLock.IsEnabled ? "On" : "Off", nameof(SecurityPage)),
        ]));
    }

    static string Count(int count, string one, string many) => $"{count} {(count == 1 ? one : many)}";

    [RelayCommand]
    Task Open(MoreRow row) => Shell.Current.GoToAsync(row.Route);
}
