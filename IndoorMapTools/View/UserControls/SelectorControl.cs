using System;
using System.ComponentModel;
using System.Linq;
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

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            //Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + 
            //    $" Selector Selection changed: {SelectedItem} ({SelectedItem?.GetType()})");

            base.OnSelectionChanged(e);

            foreach(SelectorItem item in Items.OfType<object>().Select(i => ItemContainerGenerator.ContainerFromItem(i)).OfType<SelectorItem>())
                item.IsSelected = Equals(SelectedItem, item.DataContext);
        }
        
        // TODO: 제거 확인 필요
        protected override void OnMouseMove(MouseEventArgs e) 
        {
            if(e.OriginalSource == this && Mouse.Captured == this &&
                Mouse.LeftButton != MouseButtonState.Pressed)
                ReleaseMouseCapture();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if(e.Key == Key.Delete && SelectedItem != null &&
                ItemContainerGenerator.ContainerFromItem(SelectedItem) is SelectorItem container)
                container.DeleteCommand?.Execute(null);
            e.Handled = true;
        }
    }

    public class SelectorItem : ContentControl
    {
        [Bindable(true)]
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }
        public static readonly DependencyProperty IsSelectedProperty = 
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(SelectorItem));

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
            if(ItemsControl.ItemsControlFromItemContainer(this) is Selector selector &&
                selector.ItemContainerGenerator.ItemFromContainer(this) is object item)
                selector.SetCurrentValue(Selector.SelectedItemProperty, item);
            Focus();
        }
    }
}
