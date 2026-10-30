using Kwarta.Views;

namespace Kwarta
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(TransactionEditPage), typeof(TransactionEditPage));
            Routing.RegisterRoute(nameof(CategoryEditPage), typeof(CategoryEditPage));
            Routing.RegisterRoute(nameof(AccountEditPage), typeof(AccountEditPage));
            Routing.RegisterRoute(nameof(ReportsPage), typeof(ReportsPage));
            Routing.RegisterRoute(nameof(RecurringPage), typeof(RecurringPage));
            Routing.RegisterRoute(nameof(TemplatesPage), typeof(TemplatesPage));
            Routing.RegisterRoute(nameof(GoalsPage), typeof(GoalsPage));
            Routing.RegisterRoute(nameof(GoalEditPage), typeof(GoalEditPage));
            Routing.RegisterRoute(nameof(DebtsPage), typeof(DebtsPage));
            Routing.RegisterRoute(nameof(DebtEditPage), typeof(DebtEditPage));
            Routing.RegisterRoute(nameof(BackupPage), typeof(BackupPage));
            Routing.RegisterRoute(nameof(SecurityPage), typeof(SecurityPage));
        }
    }
}
