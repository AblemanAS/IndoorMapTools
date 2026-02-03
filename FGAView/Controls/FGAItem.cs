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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace FGAView.Controls
{
    public class FGAItem : ContentControl
    {
        public FGAItem() => Cursor = Cursors.Hand;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if(ItemsControl.ItemsControlFromItemContainer(this) is Selector selector)
                selector.SelectedItem = DataContext;
            Focus();
        }


        [Bindable(true)]
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(FGAItem),
                new FrameworkPropertyMetadata(OnIsSelectedChangedFromItemContainer) { BindsTwoWayByDefault = true });

        private static void OnIsSelectedChangedFromItemContainer(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is FGAItem instance && e.NewValue is bool value)) return;
            if(instance.isInSelectionSync) return;
            bool isSelectedPrev = Selector.GetIsSelected(instance);
            if(isSelectedPrev != value)
            {
                instance.isInSelectionSync = true;
                try { Selector.SetIsSelected(instance, value); }
                finally { instance.isInSelectionSync = false; }
            }
        }

        private bool isInSelectionSync = false;

        static FGAItem() => Selector.IsSelectedProperty.OverrideMetadata(typeof(FGAItem),
                new FrameworkPropertyMetadata(OnIsSelectedChangedFromSelector));

        private static void OnIsSelectedChangedFromSelector(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is FGAItem instance && e.NewValue is bool value)) return;
            if(instance.isInSelectionSync) return;
            if(instance.IsSelected != value)
            {
                instance.isInSelectionSync = true;
                try { instance.IsSelected = value; }
                finally { instance.isInSelectionSync = false; }
            }
        }

    }
}
