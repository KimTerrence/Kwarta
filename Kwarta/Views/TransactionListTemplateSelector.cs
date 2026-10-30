using Kwarta.ViewModels;

namespace Kwarta.Views;

/// <summary>Picks the day-header or transaction template for rows of the transactions list.</summary>
public class TransactionListTemplateSelector : DataTemplateSelector
{
    public DataTemplate? HeaderTemplate { get; set; }
    public DataTemplate? ItemTemplate { get; set; }

    protected override DataTemplate? OnSelectTemplate(object item, BindableObject container) =>
        item is TransactionDayHeader ? HeaderTemplate : ItemTemplate;
}
