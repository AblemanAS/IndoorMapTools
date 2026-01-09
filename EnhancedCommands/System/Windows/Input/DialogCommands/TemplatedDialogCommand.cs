using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

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

        /// <summary>
        /// 창 생성 및 바인딩 후, 창 띄우기 직전 실행되는 커맨드
        /// </summary>
        [Bindable(true)]
        public ICommand OnOpeningCommand
        {
            get => (ICommand)GetValue(OnOpeningCommandProperty);
            set => SetValue(OnOpeningCommandProperty, value);
        }
        public static readonly DependencyProperty OnOpeningCommandProperty =
            DependencyProperty.Register(nameof(OnOpeningCommand), typeof(ICommand), typeof(TemplatedDialogCommand));
        //, new FrameworkPropertyMetadata(OnFrameworkElementMemberChanged));

        [Bindable(true)]
        public object OnOpeningCommandParameter
        {
            get => GetValue(OnOpeningCommandParameterProperty);
            set => SetValue(OnOpeningCommandParameterProperty, value);
        }
        public static readonly DependencyProperty OnOpeningCommandParameterProperty =
            DependencyProperty.Register(nameof(OnOpeningCommandParameter), typeof(object), typeof(TemplatedDialogCommand));

        /// <summary>
        /// 창 닫힌 후 실행되는 커맨드
        /// </summary>
        [Bindable(true)]
        public ICommand OnClosedCommand
        {
            get => (ICommand)GetValue(OnClosedCommandProperty);
            set => SetValue(OnClosedCommandProperty, value);
        }
        public static readonly DependencyProperty OnClosedCommandProperty = 
            DependencyProperty.Register(nameof(OnClosedCommand), typeof(ICommand), typeof(TemplatedDialogCommand));

        [Bindable(true)]
        public object OnClosedCommandParameter
        {
            get => GetValue(OnClosedCommandParameterProperty);
            set => SetValue(OnClosedCommandParameterProperty, value);
        }
        public static readonly DependencyProperty OnClosedCommandParameterProperty = 
            DependencyProperty.Register(nameof(OnClosedCommandParameter), typeof(object), typeof(TemplatedDialogCommand));


        protected virtual Window CreateDialog()
        {
            var dialog = new Window
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                Owner = Application.Current.MainWindow,
                Background = SystemColors.MenuBarBrush,
            };

            dialog.SetBinding(Window.TitleProperty, new Binding(nameof(Title)) { Source = this });
            dialog.SetBinding(Window.DataContextProperty, new Binding(nameof(DialogContext)) { Source = this });

            // OnClosedCommand Attach
            void onClosedHandler(object sender, EventArgs e)
            {
                dialog.Closed -= onClosedHandler;
                var cmd = OnClosedCommand;
                var param = OnClosedCommandParameter;
                if(cmd != null && cmd.CanExecute(param))
                    cmd.Execute(param);
            }

            dialog.Closed += onClosedHandler;
            return dialog;
        }

        protected ContentPresenter CreateTemplatedPresenter()
        {
            var templatedPresenter = new ContentPresenter { ContentTemplate = ContentTemplate };
            templatedPresenter.SetBinding(ContentPresenter.ContentProperty, new Binding());
            return templatedPresenter;
        }

        protected virtual void OnOpening()
        {
            // OnOpeningCommand 실행
            var cmd = OnOpeningCommand;
            var param = OnOpeningCommandParameter;
            if(cmd != null && cmd.CanExecute(param))
                cmd.Execute(param);
        }


        protected override void Open()
        {
            if(ContentTemplate == null) return;

            var dialog = CreateDialog();
            dialog.Content = CreateTemplatedPresenter();

            OnOpening();
            dialog.ShowDialog();
        }


        public override bool CanExecute(object parameter) => true; // 항상 실행 가능
        protected override Freezable CreateInstanceCore() => new TemplatedDialogCommand();
    }
}
