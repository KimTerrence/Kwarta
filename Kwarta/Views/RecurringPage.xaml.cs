using Kwarta.ViewModels;

namespace Kwarta.Views;

public partial class RecurringPage : ContentPage
{
    readonly RecurringListViewModel _viewModel;

    public RecurringPage(RecurringListViewModel viewModel)
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
