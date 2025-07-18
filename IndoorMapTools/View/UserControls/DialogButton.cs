using System.Windows;
using System.Windows.Controls;

namespace IndoorMapTools.View.UserControls
{
    public abstract class DialogButton : Button
    {
        protected DialogButton()
        {
            Width = 70;
            Height = 25;
            Margin = new System.Windows.Thickness(5, 5, 0, 0);
        }
    }

    public class OKButton : DialogButton
    {
        public OKButton() => IsDefault = true;

        protected override void OnClick()
        {
            Window.GetWindow(this).DialogResult = true;
            base.OnClick();
        }
    }

    public class CancelButton : DialogButton
    {
        public CancelButton() => IsCancel = true;
    }
}
