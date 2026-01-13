using System;
using System.Windows;
using System.Windows.Input;

namespace IndoorMapTools.View.UserControls
{
    public static class FileDropBehavior
    {
        public static void SetFileDropCommand(DependencyObject d, ICommand value)
            => d.SetValue(FileDropCommandProperty, value);
        public static ICommand GetFileDropCommand(DependencyObject d)
            => (ICommand)d.GetValue(FileDropCommandProperty);
        public static readonly DependencyProperty FileDropCommandProperty = DependencyProperty.RegisterAttached(
                "FileDropCommand", typeof(ICommand), typeof(FileDropBehavior), new PropertyMetadata(null, OnChanged));

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is not UIElement ui) return;
            ui.AllowDrop = true;
            ui.Drop -= OnDrop;
            if(e.NewValue != null) ui.Drop += OnDrop;
        }


        private static void OnDrop(object sender, DragEventArgs e)
        {
            if(e.Data.GetData(DataFormats.FileDrop) is string[] filePaths)
            {
                var cmd = GetFileDropCommand((DependencyObject)sender);
                if(cmd != null && cmd.CanExecute(filePaths))
                    cmd.Execute(filePaths);
            }
        }

    }

}
