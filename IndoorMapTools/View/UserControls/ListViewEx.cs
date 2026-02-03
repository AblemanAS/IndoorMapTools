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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IndoorMapTools.View.UserControls
{
    public class ListViewEx : ListView
    {
        protected override DependencyObject GetContainerForItemOverride() => new EnhancedListViewItem();
        protected override bool IsItemItsOwnContainerOverride(object item) => item is EnhancedListViewItem;

        [Bindable(true)]
        public ICommand OnFileDropCommand
        {
            get => (ICommand)GetValue(OnFileDropCommandProperty);
            set => SetValue(OnFileDropCommandProperty, value);
        }
        public static readonly DependencyProperty OnFileDropCommandProperty =
            DependencyProperty.Register(nameof(OnFileDropCommand), typeof(ICommand), typeof(ListViewEx));

        [Bindable(true)]
        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(object), typeof(ListViewEx));

        protected override void OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)
            => gridViewColumn.CellTemplate = newItemTemplate;

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if(e.Key == Key.Delete && ReorderedItem == null)
            {
                foreach(var selectedItem in new List<object>((IList<object>)SelectedItems))
                {
                    var selectedContainer = (EnhancedListViewItem)ItemContainerGenerator.ContainerFromItem(selectedItem);
                    selectedContainer.DeleteCommand?.Execute(null);
                }
            }
        }
        
        private readonly DragBoxSupport dragBox;
        private readonly GridViewColumn gridViewColumn;
        internal EnhancedListViewItem ReorderedItem { get; set; }

        public ListViewEx()
        {
            AllowDrop = true;

            // Single column GridView 설정
            GridView gridView = new GridView { AllowsColumnReorder = false };
            View = gridView;

            // Single column GridViewColumn 설정
            gridViewColumn = new GridViewColumn
            {
                HeaderContainerStyle = new Style
                {
                    TargetType = typeof(GridViewColumnHeader),
                    Setters = { new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch) }
                }
            };

            Binding headerBinding = new Binding(nameof(Header)) { Source = this };
            Binding widthBinding = new Binding(nameof(ActualWidth)) { Source = this };
            BindingOperations.SetBinding(gridViewColumn, GridViewColumn.HeaderProperty, headerBinding);
            BindingOperations.SetBinding(gridViewColumn, GridViewColumn.WidthProperty, widthBinding);
            gridView.Columns.Add(gridViewColumn);

            dragBox = new DragBoxSupport();
            dragBox.SizeChanged += OnDragBox;

            Loaded += (sender, e) => ((Grid)VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(GetVisualChild(0), 0), 0)).Children.Add(dragBox);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            dragBox?.OnParentMouseButtonDown(this, e);
            CaptureMouse();
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if(IsMouseCaptured) dragBox?.OnParentMouseMove(this, e);
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            dragBox?.OnParentMouseButtonUp(this, e);
            ReleaseMouseCapture();
            base.OnMouseLeftButtonDown(e);
        }

        private void OnDragBox(Rect rect)
        {
            foreach(var curItem in Items)
            {
                var curContainer = (EnhancedListViewItem)ItemContainerGenerator.ContainerFromItem(curItem);
                double top = curContainer.TranslatePoint(new Point(0, 0), this).Y;
                double bottom = top + curContainer.ActualHeight;
                curContainer.IsSelected = !(bottom < rect.Top || top > rect.Bottom);
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if(OnFileDropCommand != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                //string ERR_MSG_FILE = "다음 파일 로드 중 오류가 발생했습니다\n";
                //string ERR_TITLE_FILE = "파일 로드 중 오류 발생";

                foreach(string filePath in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    OnFileDropCommand.Execute(filePath);// (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
                }
            }
        }
    }


    public class EnhancedListViewItem : ListViewItem
    {
        [Bindable(true)]
        public ICommand DeleteCommand
        {
            get => (ICommand)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }
        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(EnhancedListViewItem));

        [Bindable(true)]
        public ICommand ReorderCommand
        {
            get => (ICommand)GetValue(ReorderCommandProperty);
            set => SetValue(ReorderCommandProperty, value);
        }
        public static readonly DependencyProperty ReorderCommandProperty =
            DependencyProperty.Register(nameof(ReorderCommand), typeof(ICommand), typeof(EnhancedListViewItem));

        [Bindable(true)]
        public bool UpperCaretVisible
        {
            get => (bool)GetValue(UpperCaretVisibleProperty);
            set => SetValue(UpperCaretVisibleProperty, value);
        }
        public static readonly DependencyProperty UpperCaretVisibleProperty =
            DependencyProperty.Register(nameof(UpperCaretVisible), typeof(bool), typeof(EnhancedListViewItem));

        [Bindable(true)]
        public bool LowerCaretVisible
        {
            get => (bool)GetValue(LowerCaretVisibleProperty);
            set => SetValue(LowerCaretVisibleProperty, value);
        }
        public static readonly DependencyProperty LowerCaretVisibleProperty =
            DependencyProperty.Register(nameof(LowerCaretVisible), typeof(bool), typeof(EnhancedListViewItem));

        private ListViewEx parentList;
        private Rectangle upperCaret; // 하단 divider
        private Rectangle lowerCaret; // 상단 divider

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // 기존 Border 추출
            Border outerBorder = VisualTreeHelper.GetChild(this, 0) as Border;
            Border innerBorder = VisualTreeHelper.GetChild(outerBorder, 0) as Border;

            // Separator 생성 및 Margin 조정
            outerBorder.Margin = new Thickness(0, 2, 0, 0);
            upperCaret = new Rectangle { Height = 1, HorizontalAlignment = HorizontalAlignment.Stretch, Fill = Brushes.Black };
            upperCaret.Margin = new Thickness(0, -4, 0, 1);
            upperCaret.SetBinding(Rectangle.VisibilityProperty, new Binding(nameof(UpperCaretVisible))
            { Source = this, Converter = new IsVisibleToVisibilityConverter() });
            lowerCaret = new Rectangle { Height = 1, HorizontalAlignment = HorizontalAlignment.Stretch, Fill = Brushes.Black };
            lowerCaret.Margin = new Thickness(0, 1, 0, -4);
            lowerCaret.SetBinding(Rectangle.VisibilityProperty, new Binding(nameof(LowerCaretVisible))
            { Source = this, Converter = new IsVisibleToVisibilityConverter() });

            // 스택패널에 배치
            StackPanel rtPanel = new StackPanel { Orientation = Orientation.Vertical };
            outerBorder.Child = rtPanel;
            rtPanel.Children.Add(upperCaret);
            rtPanel.Children.Add(innerBorder);
            rtPanel.Children.Add(lowerCaret);
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            for(DependencyObject cur = this; cur != null; cur = VisualTreeHelper.GetParent(cur))
                if(cur is ListViewEx listView) parentList = listView;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if(parentList.SelectedItem == DataContext)
            {
                CaptureMouse();
                parentList.ReorderedItem = this;
                e.Handled = true;
            }
            else
            {
                base.OnMouseLeftButtonDown(e);
                e.Handled = false;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if(parentList.ReorderedItem == null || !parentList.ReorderedItem.IsMouseCaptured) return; // Reorder 중이 아니면 return
            if(parentList.Items.Count < 1) return;  // Item이 아예 없으면 return

            var destContainer = (EnhancedListViewItem)parentList.ItemContainerGenerator.ContainerFromItem(parentList.Items[0]);

            for(int i = 0; i < parentList.Items.Count; i++)
            {
                var curContainer = (EnhancedListViewItem)parentList.ItemContainerGenerator.ContainerFromItem(parentList.Items[i]);
                curContainer.SetCurrentValue(UpperCaretVisibleProperty, false);
                curContainer.SetCurrentValue(LowerCaretVisibleProperty, false);
                if(e.GetPosition(curContainer).Y >= 0) destContainer = curContainer;
            }

            if(e.GetPosition(destContainer).Y > destContainer.ActualHeight / 2)
            {
                destContainer.SetCurrentValue(UpperCaretVisibleProperty, false);
                destContainer.SetCurrentValue(LowerCaretVisibleProperty, true);
            }
            else
            {
                destContainer.SetCurrentValue(UpperCaretVisibleProperty, true);
                destContainer.SetCurrentValue(LowerCaretVisibleProperty, false);
            }
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);

            if(parentList.ReorderedItem == this)
            {
                parentList.ReorderedItem = null;
                if(parentList.Items.Count < 1) return;  // Item이 아예 없으면 return
                object thisItem = DataContext;

                int sourceIndex = parentList.Items.IndexOf(thisItem);
                int destIndex = 0;
                var destContainer = (EnhancedListViewItem)parentList.ItemContainerGenerator.ContainerFromItem(parentList.Items[0]);
                destContainer.SetCurrentValue(UpperCaretVisibleProperty, false);
                destContainer.SetCurrentValue(LowerCaretVisibleProperty, false);

                for(int i = 0; i < parentList.Items.Count; i++)
                {
                    var curContainer = (EnhancedListViewItem)parentList.ItemContainerGenerator.ContainerFromItem(parentList.Items[i]);
                    curContainer.SetCurrentValue(UpperCaretVisibleProperty, false);
                    curContainer.SetCurrentValue(LowerCaretVisibleProperty, false);
                    if(e.GetPosition(curContainer).Y >= 0)
                    {
                        destContainer = curContainer;
                        destIndex = i;
                    }
                }

                if(sourceIndex == destIndex) return; // Destination이 자신일 경우 return
                if(destIndex > sourceIndex) destIndex--;    // remove 후 insert 고려 index 조정
                if(e.GetPosition(destContainer).Y > destContainer.ActualHeight / 2) destIndex++;  // 위아래 결정
                if(sourceIndex == destIndex) return; // Destination이 자신일 경우 return

                if(parentList.ItemsSource == null)
                {
                    parentList.Items.RemoveAt(sourceIndex);
                    parentList.Items.Insert(destIndex, thisItem);
                }
                else ReorderCommand?.Execute(destIndex);
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            ReleaseMouseCapture();
        }

        private class IsVisibleToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                => ((bool)value) ? Visibility.Visible : Visibility.Hidden;

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                => (Visibility)value == Visibility.Visible;
        }
    }
}
