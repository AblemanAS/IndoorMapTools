using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace EnhancedCommands.System.Windows.Input.DialogCommands
{
    public abstract class FileDialogCommand : DialogCommandBase, ICommandSource
    {
        [Bindable(true)]
        public string Filter
        {
            get => (string)GetValue(FilterProperty);
            set => SetValue(FilterProperty, value);
        }
        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register(nameof(Filter), typeof(string), typeof(FileDialogCommand));

        [Bindable(true)]
        public string InitialDirectory
        {
            get => (string)GetValue(InitialDirectoryProperty);
            set => SetValue(InitialDirectoryProperty, value);
        }
        public static readonly DependencyProperty InitialDirectoryProperty =
            DependencyProperty.Register(nameof(InitialDirectory), typeof(string), typeof(FileDialogCommand),
                new FrameworkPropertyMetadata(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));

        [Bindable(true)]
        public string FileName
        {
            get => (string)GetValue(FileNameProperty);
            set => SetValue(FileNameProperty, value);
        }
        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register(nameof(FileName), typeof(string), typeof(FileDialogCommand),
                new FrameworkPropertyMetadata(""));

        [Bindable(true)]
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand),
            typeof(FileDialogCommand));//, new FrameworkPropertyMetadata(OnFrameworkElementMemberChanged));

        [Bindable(true)]
        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(FileDialogCommand));

        [Bindable(true)]
        public IInputElement CommandTarget
        {
            get => (IInputElement)GetValue(CommandTargetProperty);
            set => SetValue(CommandTargetProperty, value);
        }
        public static readonly DependencyProperty CommandTargetProperty =
            DependencyProperty.Register(nameof(CommandTarget), typeof(IInputElement), typeof(FileDialogCommand));


        protected override void Open()
        {
            var dialog = CreateDialog();
            if(Filter != null) dialog.Filter = Filter;
            dialog.Title = Title;
            dialog.InitialDirectory = InitialDirectory;
            bool? result = dialog.ShowDialog();
            SetCurrentValue(FileNameProperty, dialog.FileName);
            if(result == true) Command?.Execute(CommandParameter);
        }

        protected abstract FileDialog CreateDialog();

        // 단일 커맨드 패턴이므로 실행 대상 커맨드를 미러
        public override bool CanExecute(object parameter)
            => Command == null || Command.CanExecute(CommandParameter);
    }


    public class OpenFileDialogCommand : FileDialogCommand
    {
        protected override FileDialog CreateDialog() => new OpenFileDialog();
        protected override Freezable CreateInstanceCore() => new OpenFileDialogCommand();
    }

    public class SaveFileDialogCommand : FileDialogCommand
    {
        protected override FileDialog CreateDialog() => new SaveFileDialog();
        protected override Freezable CreateInstanceCore() => new SaveFileDialogCommand();
    }
}
