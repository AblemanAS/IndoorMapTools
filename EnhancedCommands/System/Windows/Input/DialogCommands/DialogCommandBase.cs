using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace EnhancedCommands.System.Windows.Input.DialogCommands
{
    public abstract class DialogCommandBase : ContextCommand
    {
        [Bindable(true)]
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string),
            typeof(DialogCommandBase), new FrameworkPropertyMetadata(""));

        public override void Execute(object parameter) => Open();
        protected abstract void Open();
    }
}
