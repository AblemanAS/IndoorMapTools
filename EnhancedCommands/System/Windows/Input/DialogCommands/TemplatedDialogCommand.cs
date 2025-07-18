using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace EnhancedCommands.System.Windows.Input.DialogCommands
{
    [ContentProperty(nameof(ContentTemplate))]
    public class TemplatedDialogCommand : DialogCommandBase
    {
        [Bindable(true)]
        public DataTemplate ContentTemplate
        {
            get => (DataTemplate)GetValue(ContentTemplateProperty);
            set => SetValue(ContentTemplateProperty, value);
        }
        public static readonly DependencyProperty ContentTemplateProperty =
            DependencyProperty.Register(nameof(ContentTemplate), typeof(DataTemplate), typeof(TemplatedDialogCommand));

        [Bindable(true)]
        public object DialogContext
        {
            get => GetValue(DialogContextProperty);
            set => SetValue(DialogContextProperty, value);
        }
        public static readonly DependencyProperty DialogContextProperty =
            DependencyProperty.Register(nameof(DialogContext), typeof(object), typeof(TemplatedDialogCommand),
                new FrameworkPropertyMetadata { DefaultValue = null });

        protected override void Open()
        {
            if(ContentTemplate == null) return;

            var dialog = new Window
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                Owner = Application.Current.MainWindow,
                Background = SystemColors.MenuBarBrush,
                ContentTemplate = ContentTemplate
            };

            dialog.SetBinding(Window.ContentProperty, new Binding(nameof(DialogContext)) { Source = this });
            dialog.SetBinding(Window.TitleProperty, new Binding(nameof(Title)) { Source = this });
            dialog.ShowDialog();
        }


        protected override Freezable CreateInstanceCore() => new TemplatedDialogCommand();
    }
}
