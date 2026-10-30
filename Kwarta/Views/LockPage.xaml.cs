using Kwarta.ViewModels;

namespace Kwarta.Views;

/// <summary>Full-screen PIN pad shown modally over the app while it is locked.</summary>
public partial class LockPage : ContentPage
{
    readonly LockViewModel _viewModel;
    bool _promptedBiometrics;

    public LockPage(LockViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        viewModel.Unlocked += (_, _) => Unlocked?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Unlocked;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_promptedBiometrics)
            return;
        _promptedBiometrics = true;
        await _viewModel.UseBiometrics();
    }

    // Back must not dismiss the lock.
    protected override bool OnBackButtonPressed() => true;
}
