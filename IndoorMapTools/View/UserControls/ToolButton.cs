using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using MapView.System.Windows.Controls;

namespace IndoorMapTools.View.UserControls
{
    public class ToolButton : ToggleButton
    {
        public bool MatchByFamily { get; set; }

        public MouseTool Tool
        {
            get => (MouseTool)GetValue(ToolProperty);
            set => SetValue(ToolProperty, value);
        }

        public static readonly DependencyProperty ToolProperty =
            DependencyProperty.Register(nameof(Tool), typeof(MouseTool), typeof(ToolButton),
                new FrameworkPropertyMetadata(null, OnToolChanged));

        private static void OnToolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is ToolButton tb)) return;
            if(e.NewValue is DependencyObject toolObj)
                toolObj.SetValue(DataContextProperty, tb.DataContext);
        }

        [Bindable(true)]
        public MouseTool ActivationTarget
        {
            get => (MouseTool)GetValue(ActivationTargetProperty);
            set => SetValue(ActivationTargetProperty, value);
        }
        public static readonly DependencyProperty ActivationTargetProperty =
            DependencyProperty.Register(nameof(ActivationTarget), typeof(MouseTool), typeof(ToolButton),
                new FrameworkPropertyMetadata(OnActivationTargetChanged) { BindsTwoWayByDefault = true });

        private static void OnActivationTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is ToolButton instance)) return;
            if(instance.MatchByFamily)
            {
                if((e.NewValue as MouseTool)?.ToolName != instance.Tool?.ToolName && (bool)instance.IsChecked)
                    instance.SetValue(ToolButton.IsCheckedProperty, false);
                else if((e.NewValue as MouseTool)?.ToolName == instance.Tool?.ToolName && !(bool)instance.IsChecked)
                    instance.SetValue(ToolButton.IsCheckedProperty, true);
            }
            else
            {
                if(e.NewValue != instance.Tool && (bool)instance.IsChecked)
                    instance.SetValue(ToolButton.IsCheckedProperty, false);
                else if(e.NewValue == instance.Tool && !(bool)instance.IsChecked)
                    instance.SetValue(ToolButton.IsCheckedProperty, true); 
            }
        }

        public ToolButton()
        {
            IsVisibleChanged += (sender, e) => { if(!IsVisible) SetValue(ToolButton.IsCheckedProperty, false); };
            DataContextChanged += (sender, e) => 
            {
                SetValue(ToolButton.IsCheckedProperty, false);
                Tool?.SetValue(DataContextProperty, DataContext);
            };
            IsTabStop = false;
        }

        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);

            if(MatchByFamily)
            {
                if(ActivationTarget?.ToolName != Tool?.ToolName)
                    SetValue(ActivationTargetProperty, Tool);
            }
            else
            {
                if(ActivationTarget != Tool)
                    SetValue(ActivationTargetProperty, Tool);
            }
        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);

            if(MatchByFamily)
            {
                if(ActivationTarget?.ToolName == Tool?.ToolName)
                    SetValue(ActivationTargetProperty, null);
            }
            else
            {
                if(ActivationTarget == Tool)
                    SetValue(ActivationTargetProperty, null);
            }
        }
    }
}
