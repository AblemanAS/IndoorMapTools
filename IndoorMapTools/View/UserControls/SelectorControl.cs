using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace IndoorMapTools.View.UserControls
{
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(SelectorItem))]
    public class SelectorControl : Selector
    {
        protected override bool IsItemItsOwnContainerOverride(object item) => item is SelectorItem;
        protected override DependencyObject GetContainerForItemOverride() => new SelectorItem();

        public SelectorControl() => IsTabStop = false;

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if(e.Key == Key.Delete && SelectedItem != null &&
                ItemContainerGenerator.ContainerFromItem(SelectedItem) is SelectorItem container)
                container.DeleteCommand?.Execute(null);
        }
    }


    public class SelectorItem : ContentControl
    {
        [Bindable(true)]
        public ICommand DeleteCommand
        {
            get => (ICommand)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }
        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(SelectorItem));

        public SelectorItem() => Cursor = Cursors.Hand;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if(ItemsControl.ItemsControlFromItemContainer(this) is Selector selector)
                selector.SelectedItem = DataContext;
            Focus();
        }
    }
}
