/********************************************************************************
Copyright 2026-present Korea Advanced Institute of Science and Technology (KAIST)

Author: Kyuho Son

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
********************************************************************************/

using System.ComponentModel;
using System.Windows.Input;
using System.Windows;

namespace EnhancedCommands.Input.DialogCommands
{
    public class MessageBoxCommand : DialogCommandBase, ICommandSource
    {
        [Bindable(true)]
        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }
        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(nameof(Message), typeof(string),
            typeof(MessageBoxCommand), new FrameworkPropertyMetadata(""));

        [Bindable(true)]
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand),
            typeof(MessageBoxCommand));//, new FrameworkPropertyMetadata(OnFrameworkElementMemberChanged));

        [Bindable(true)]
        public ICommand NoCommand
        {
            get => (ICommand)GetValue(NoCommandProperty);
            set => SetValue(NoCommandProperty, value);
        }
        public static readonly DependencyProperty NoCommandProperty = DependencyProperty.Register(nameof(NoCommand), typeof(ICommand),
            typeof(MessageBoxCommand));//, new FrameworkPropertyMetadata(OnFrameworkElementMemberChanged));

        [Bindable(true)]
        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(MessageBoxCommand));

        [Bindable(true)]
        public IInputElement CommandTarget
        {
            get => (IInputElement)GetValue(CommandTargetProperty);
            set => SetValue(CommandTargetProperty, value);
        }
        public static readonly DependencyProperty CommandTargetProperty =
            DependencyProperty.Register(nameof(CommandTarget), typeof(IInputElement), typeof(MessageBoxCommand));

        public MessageBoxButton Buttons { get; set; } = 0;
        public MessageBoxImage Icon { get; set; } = 0;
        public MessageBoxResult DefaultResult { get; set; } = 0;

        protected override void Open()
        {
            MessageBoxResult result = MessageBox.Show(Message, Title, Buttons, Icon, DefaultResult);
            if(result == MessageBoxResult.Yes) Command?.Execute(CommandParameter);
            else if(result == MessageBoxResult.No) NoCommand?.Execute(CommandParameter);
        }

        public override bool CanExecute(object parameter) => true;
        protected override Freezable CreateInstanceCore() => new MessageBoxCommand();
    }
}
