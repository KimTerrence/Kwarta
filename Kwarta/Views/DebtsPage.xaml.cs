using Kwarta.ViewModels;

namespace Kwarta.Views;

public partial class DebtsPage : ContentPage
{
    readonly DebtsViewModel _viewModel;

    public DebtsPage(DebtsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
