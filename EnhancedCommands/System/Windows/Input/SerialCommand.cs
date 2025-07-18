using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace EnhancedCommands.System.Windows.Input
{
    public class SerialCommand : ContextCommand
    {
        [Bindable(true)]
        public ICommand Command1
        {
            get => (ICommand)GetValue(Command1Property);
            set => SetValue(Command1Property, value);
        }
        public static readonly DependencyProperty Command1Property = DependencyProperty.Register(nameof(Command1), 
            typeof(ICommand), typeof(SerialCommand));

        [Bindable(true)]
        public ICommand Command2
        {
            get => (ICommand)GetValue(Command2Property);
            set => SetValue(Command2Property, value);
        }
        public static readonly DependencyProperty Command2Property = DependencyProperty.Register(nameof(Command2),
            typeof(ICommand), typeof(SerialCommand));

        [Bindable(true)]
        public ICommand Command3
        {
            get => (ICommand)GetValue(Command3Property);
            set => SetValue(Command3Property, value);
        }
        public static readonly DependencyProperty Command3Property = DependencyProperty.Register(nameof(Command3),
            typeof(ICommand), typeof(SerialCommand));

        [Bindable(true)]
        public ICommand Command4
        {
            get => (ICommand)GetValue(Command4Property);
            set => SetValue(Command4Property, value);
        }
        public static readonly DependencyProperty Command4Property = DependencyProperty.Register(nameof(Command4),
            typeof(ICommand), typeof(SerialCommand));

        [Bindable(true)]
        public ICommand Command5
        {
            get => (ICommand)GetValue(Command5Property);
            set => SetValue(Command5Property, value);
        }
        public static readonly DependencyProperty Command5Property = DependencyProperty.Register(nameof(Command5),
            typeof(ICommand), typeof(SerialCommand));

        [Bindable(true)]
        public object CommandParameter1
        {
            get => GetValue(CommandParameter1Property);
            set => SetValue(CommandParameter1Property, value);
        }
        public static readonly DependencyProperty CommandParameter1Property = DependencyProperty.Register(nameof(CommandParameter1),
            typeof(object), typeof(SerialCommand));

        [Bindable(true)]
        public object CommandParameter2
        {
            get => GetValue(CommandParameter2Property);
            set => SetValue(CommandParameter2Property, value);
        }
        public static readonly DependencyProperty CommandParameter2Property = DependencyProperty.Register(nameof(CommandParameter2),
            typeof(object), typeof(SerialCommand));

        [Bindable(true)]
        public object CommandParameter3
        {
            get => GetValue(CommandParameter3Property);
            set => SetValue(CommandParameter3Property, value);
        }
        public static readonly DependencyProperty CommandParameter3Property = DependencyProperty.Register(nameof(CommandParameter3),
            typeof(object), typeof(SerialCommand));

        [Bindable(true)]
        public object CommandParameter4
        {
            get => GetValue(CommandParameter4Property);
            set => SetValue(CommandParameter4Property, value);
        }
        public static readonly DependencyProperty CommandParameter4Property = DependencyProperty.Register(nameof(CommandParameter4),
            typeof(object), typeof(SerialCommand));

        [Bindable(true)]
        public object CommandParameter5
        {
            get => GetValue(CommandParameter5Property);
            set => SetValue(CommandParameter5Property, value);
        }
        public static readonly DependencyProperty CommandParameter5Property = DependencyProperty.Register(nameof(CommandParameter5),
            typeof(object), typeof(SerialCommand));

        public static DependencyProperty[] CommandProperties { get; } = new DependencyProperty[]
        { Command1Property, Command2Property, Command3Property, Command4Property, Command5Property };

        public static DependencyProperty[] CommandParameterProperties { get; } = new DependencyProperty[]
        { CommandParameter1Property, CommandParameter2Property, CommandParameter3Property, CommandParameter4Property, CommandParameter5Property };

        public override void Execute(object parameter)
        {
            for(int i = 0; i < CommandProperties.Length; i++)
                (GetValue(CommandProperties[i]) as ICommand)?.Execute(GetValue(CommandParameterProperties[i]));
        }

        protected override Freezable CreateInstanceCore() => new SerialCommand();
    }
}
