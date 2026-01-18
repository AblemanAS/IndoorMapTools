/***********************************************************************
Copyright 2026-present Kyuho Son

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
***********************************************************************/

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace IndoorMapTools.View.UserControls
{
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(SelectorItem))]
    public class SelectorControl : Selector
    {
        protected override bool IsItemItsOwnContainerOverride(object item) => item is SelectorItem;
        protected override DependencyObject GetContainerForItemOverride() => new SelectorItem();

        public SelectorControl()
        {
            ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(Grid)));
            IsTabStop = false;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if(e.Key == Key.Delete && SelectedItem != null &&
                ItemContainerGenerator.ContainerFromItem(SelectedItem) is SelectorItem container)
                container.DeleteCommand?.Execute(null);
        }
    }


    public class SelectorItem : ContentControl
    {
        [Bindable(true)]
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(SelectorItem),
                new FrameworkPropertyMetadata(OnIsSelectedChangedFromItemContainer) { BindsTwoWayByDefault = true });

        private static void OnIsSelectedChangedFromItemContainer(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is not SelectorItem instance || e.NewValue is not bool value) return;
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

        static SelectorItem()
        {
            Selector.IsSelectedProperty.OverrideMetadata(typeof(SelectorItem),
                new FrameworkPropertyMetadata(OnIsSelectedChangedFromSelector));
        }

        private static void OnIsSelectedChangedFromSelector(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is not SelectorItem instance || e.NewValue is not bool value) return;
            if(instance.isInSelectionSync) return;
            if(instance.IsSelected != value)
            {
                instance.isInSelectionSync = true;
                try { instance.IsSelected = value; }
                finally { instance.isInSelectionSync = false; }
            }
        }

        [Bindable(true)]
        public ICommand DeleteCommand
        {
            get => (ICommand)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }
        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(SelectorItem));

        public SelectorItem() => Cursor = Cursors.Hand;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if(ItemsControl.ItemsControlFromItemContainer(this) is Selector selector)
                selector.SelectedItem = DataContext;
            Focus();
        }
    }
}
