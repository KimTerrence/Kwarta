using Kwarta.ViewModels;

namespace Kwarta.Views;

public partial class SecurityPage : ContentPage
{
    readonly SecurityViewModel _viewModel;

    public SecurityPage(SecurityViewModel viewModel)
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
