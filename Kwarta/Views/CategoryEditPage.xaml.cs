using Kwarta.ViewModels;

namespace Kwarta.Views;

public partial class CategoryEditPage : ContentPage
{
    public CategoryEditPage(CategoryEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
