using Kwarta.ViewModels;

namespace Kwarta.Views;

public partial class TransactionEditPage : ContentPage
{
    public TransactionEditPage(TransactionEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
