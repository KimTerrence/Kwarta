using Kwarta.ViewModels;

namespace Kwarta.Views;

public partial class DashboardPage : ContentPage
{
    readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // The Overview is the first page, so it hosts the launch animation over itself.
        if (SplashOverlay.HasPlayed)
            Splash.IsVisible = false;
        else
            Shell.SetTabBarIsVisible(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var loading = _viewModel.LoadAsync();
        if (Splash.IsVisible)
            await Splash.PlayAsync(loading, () => Shell.SetTabBarIsVisible(this, true));
        await loading;
    }
}
