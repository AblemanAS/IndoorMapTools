using System;
using System.ComponentModel;
using System.Windows;

namespace EnhancedCommands.System.Windows.Input
{
    public class SetPropertyCommand : ContextCommand
    {
        [Bindable(true)]
        public object TargetProperty
        {
            get => GetValue(TargetPropertyProperty);
            set => SetValue(TargetPropertyProperty, value);
        }
        public static readonly DependencyProperty TargetPropertyProperty = DependencyProperty.Register(nameof(TargetProperty),
            typeof(object), typeof(SetPropertyCommand), new FrameworkPropertyMetadata { BindsTwoWayByDefault = true });

        [Bindable(true)]
        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value),
            typeof(object), typeof(SetPropertyCommand));

        public override void Execute(object parameter)
        {
            SetCurrentValue(TargetPropertyProperty, Value);
            //if(!(DataContext != null && DataContext.GetType().GetProperty(PropertyName) is PropertyInfo info &&
            //    (Value == null || info.PropertyType == Value.GetType()))) return;
            //info.SetValue(DataContext, Value);
        }

        protected override Freezable CreateInstanceCore() => new SetPropertyCommand();
    }
}
