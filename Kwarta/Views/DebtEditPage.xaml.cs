using Kwarta.ViewModels;

namespace Kwarta.Views;

public partial class DebtEditPage : ContentPage
{
    public DebtEditPage(DebtEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
