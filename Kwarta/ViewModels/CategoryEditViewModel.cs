using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kwarta.Models;
using Kwarta.Services;

namespace Kwarta.ViewModels;

public partial class CategoryEditViewModel(BudgetDatabase database) : ObservableObject, IQueryAttributable
{
    Category _category = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial bool IsExisting { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExpense))]
    public partial TransactionType Type { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewName))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BudgetText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Glyph { get; set; } = Icons.CategoryGlyph(null);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Tint))]
    public partial Color Color { get; set; } = Color.FromArgb(Icons.CategoryColors[0]);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorText { get; set; } = string.Empty;

    public string Title => IsExisting ? "Edit Category" : "New Category";
    public bool IsExpense => Type == TransactionType.Expense;
    public bool HasError => !string.IsNullOrEmpty(ErrorText);
    public Color Tint => Color.WithAlpha(0.16f);
    public string PreviewName => string.IsNullOrWhiteSpace(Name) ? "Category name" : Name;
    public string CurrencySymbol => Money.Symbol;

    public IReadOnlyList<SelectableOption> IconOptions { get; } =
        Icons.Category.Select(i => new SelectableOption(i.Key, i.Value, Colors.Transparent)).ToList();

    public IReadOnlyList<SelectableOption> ColorOptions { get; } =
        Icons.CategoryColors.Select(hex => new SelectableOption(hex, string.Empty, Color.FromArgb(hex))).ToList();

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idValue) && idValue is int id && id != 0
            && await database.GetCategoryAsync(id) is { } existing)
        {
            _category = existing;
            IsExisting = true;
        }
        else if (query.TryGetValue("type", out var typeValue) && typeValue is TransactionType type)
        {
            _category = new Category
            {
                Type = type,
                Icon = type == TransactionType.Income ? "payments" : Icons.DefaultCategoryIcon,
                ColorHex = Icons.CategoryColors[Random.Shared.Next(Icons.CategoryColors.Count)]
            };
        }

        Type = _category.Type;
        Name = _category.Name;
        BudgetText = _category.MonthlyBudget > 0 ? _category.MonthlyBudget.ToString("0.##") : string.Empty;
        SelectIcon(IconOptions.FirstOrDefault(o => o.Key == _category.Icon) ?? IconOptions[0]);
        SelectColor(ColorOptions.FirstOrDefault(o => o.Key.Equals(_category.ColorHex, StringComparison.OrdinalIgnoreCase))
            ?? ColorOptions[0]);
    }

    [RelayCommand]
    void SelectIcon(SelectableOption option)
    {
        foreach (var o in IconOptions)
            o.IsSelected = o == option;
        Glyph = option.Glyph;
    }

    [RelayCommand]
    void SelectColor(SelectableOption option)
    {
        foreach (var o in ColorOptions)
            o.IsSelected = o == option;
        Color = option.Color;
    }

    [RelayCommand]
    async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorText = "Give the category a name.";
            return;
        }

        decimal budget = 0;
        if (IsExpense && !string.IsNullOrWhiteSpace(BudgetText)
            && (!Money.TryParse(BudgetText, out budget) || budget < 0))
        {
            ErrorText = "Enter a valid monthly budget, or leave it blank.";
            return;
        }

        _category.Name = Name.Trim();
        _category.Icon = IconOptions.First(o => o.IsSelected).Key;
        _category.ColorHex = ColorOptions.First(o => o.IsSelected).Key;
        _category.MonthlyBudget = Math.Round(budget, 2);

        await database.SaveCategoryAsync(_category);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    async Task Delete()
    {
        if (!IsExisting)
            return;

        if (await database.IsCategoryInUseAsync(_category.Id))
        {
            await Shell.Current.DisplayAlertAsync("Category in use",
                $"{_category.Name} has transactions. Move or delete them before deleting the category.", "OK");
            return;
        }

        if (!await Shell.Current.DisplayAlertAsync("Delete category", $"Delete {_category.Name}?", "Delete", "Cancel"))
            return;

        await database.DeleteCategoryAsync(_category);
        await Shell.Current.GoToAsync("..");
    }
}
