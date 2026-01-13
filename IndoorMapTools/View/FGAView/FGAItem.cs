using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace IndoorMapTools.View.FGAView
{
    public class FGAItem : ContentControl
    {
        public FGAItem() => Cursor = Cursors.Hand;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if(ItemsControl.ItemsControlFromItemContainer(this) is Selector selector)
                selector.SelectedItem = DataContext;
            Focus();
        }
    }
}
