using Kwarta.ViewModels;

namespace Kwarta.Views;

public partial class GoalEditPage : ContentPage
{
    public GoalEditPage(GoalEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
