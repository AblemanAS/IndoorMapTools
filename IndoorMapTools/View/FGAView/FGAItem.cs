using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace IndoorMapTools.View.FGAView
{
    public class FGAItem : ContentControl
    {
        [Bindable(true)]
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(FGAItem),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if(ItemsControl.ItemsControlFromItemContainer(this) is Selector selector && 
                selector.ItemContainerGenerator.ItemFromContainer(this) is object item)
                selector.SetCurrentValue(Selector.SelectedItemProperty, item);
        }
    }
}
