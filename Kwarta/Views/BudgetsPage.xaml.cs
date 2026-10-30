using Kwarta.ViewModels;

namespace Kwarta.Views;

public partial class BudgetsPage : ContentPage
{
    readonly BudgetsViewModel _viewModel;

    public BudgetsPage(BudgetsViewModel viewModel)
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
