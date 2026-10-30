using Kwarta.ViewModels;

namespace Kwarta.Views;

public partial class AccountEditPage : ContentPage
{
    public AccountEditPage(AccountEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
