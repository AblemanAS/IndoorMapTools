using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace IndoorMapTools.View.UserControls
{
    public class HierarchyCheckBox : CheckBox
    {
        [Bindable(true)]
        public HierarchyCheckBox Master
        {
            get => (HierarchyCheckBox)GetValue(MasterProperty);
            set => SetValue(MasterProperty, value);
        }

        public static readonly DependencyProperty MasterProperty =
            DependencyProperty.Register(nameof(Master), typeof(HierarchyCheckBox), typeof(HierarchyCheckBox), 
                new FrameworkPropertyMetadata(OnMasterChanged));

        private static void OnMasterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is HierarchyCheckBox instance)) return;
            (e.OldValue as HierarchyCheckBox)?.RemoveSlave(instance);
            (e.NewValue as HierarchyCheckBox)?.AddSlave(instance);
        }

        private readonly List<WeakReference<HierarchyCheckBox>> slaves = new List<WeakReference<HierarchyCheckBox>>();

        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            if(Master!= null && Master.IsChecked is bool masterIsChecked && !masterIsChecked)
                Master?.OnSlaveCheckedChanged();
            SetSlavesIsChecked(true);
        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            if(Master != null && Master.IsChecked is bool masterIsChecked && masterIsChecked)
                Master?.OnSlaveCheckedChanged();
            SetSlavesIsChecked(false);
        }

        public void AddSlave(HierarchyCheckBox slave) 
            => slaves.Add(new WeakReference<HierarchyCheckBox>(slave));

        public void RemoveSlave(HierarchyCheckBox slave)
        {
            for(int i = slaves.Count - 1; i >= 0; i--)
            {
                if(!slaves[i].TryGetTarget(out var curSlave)) slaves.RemoveAt(i);
                else if(curSlave == slave)
                {
                    slaves.RemoveAt(i);
                    return;
                }
            }
        }

        public void OnSlaveCheckedChanged()
        {
            bool selfChecked = (bool)IsChecked;
            for(int i = slaves.Count - 1; i >= 0; i--)
            {
                if(!slaves[i].TryGetTarget(out var curSlave)) slaves.RemoveAt(i);
                else if(curSlave.IsChecked == selfChecked) return;
            }

            SetCurrentValue(IsCheckedProperty, !selfChecked);
        }

        private void SetSlavesIsChecked(bool value)
        {
            bool selfChecked = (bool)IsChecked;
            for(int i = slaves.Count-1; i >= 0; i--)
            {
                if(!slaves[i].TryGetTarget(out var curSlave)) slaves.RemoveAt(i);
                else if(curSlave.IsChecked != selfChecked)
                    curSlave.SetCurrentValue(IsCheckedProperty, value);
            }
        }
    }
}
