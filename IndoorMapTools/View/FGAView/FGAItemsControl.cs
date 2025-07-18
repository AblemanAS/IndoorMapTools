using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace IndoorMapTools.View.FGAView
{
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(FGAItem))]
    public class FGAItemsControl : Selector
    {
        protected override bool IsItemItsOwnContainerOverride(object item) => item is FGAItem;
        protected override DependencyObject GetContainerForItemOverride() => new FGAItem();

        public FGAItemsControl()
        {
            ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(FGAPanel)));
            IsTabStop = false;
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            foreach(FGAItem item in Items.OfType<object>().Select(i => ItemContainerGenerator.ContainerFromItem(i)).OfType<FGAItem>())
                item.IsSelected = Equals(SelectedItem, item.DataContext);
        }
    }
}
