using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace IndoorMapTools.View
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Height = SystemParameters.PrimaryScreenHeight * 0.8;
            Width = SystemParameters.PrimaryScreenWidth * 0.9;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e) => HandleKeyEvent(e);
        protected override void OnPreviewKeyUp(KeyEventArgs e) => HandleKeyEvent(e);

        private void HandleKeyEvent(KeyEventArgs e)
        {
            if(e.Key == Key.System)
            {
                e.Handled = true;
                RaiseEvent(new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, Key.LeftAlt)
                { RoutedEvent = e.RoutedEvent });
            }
        }
    }
}
