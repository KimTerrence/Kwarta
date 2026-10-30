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
        }
    }
}
