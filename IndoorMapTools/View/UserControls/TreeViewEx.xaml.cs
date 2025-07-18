using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IndoorMapTools.View.UserControls
{
    public class TreeViewItemEx : TreeViewItem
    {
        private static TreeViewItemEx dragStartItem;
        protected override DependencyObject GetContainerForItemOverride() => new TreeViewItemEx();

        [Bindable(true)]
        public bool CanBeParent
        {
            get => (bool)GetValue(CanBeParentProperty);
            set => SetValue(CanBeParentProperty, value);
        }
        public static readonly DependencyProperty CanBeParentProperty =
            DependencyProperty.Register(nameof(CanBeParent), typeof(bool), typeof(TreeViewItemEx));

        [Bindable(true)]
        public bool CanBeChild
        {
            get => (bool)GetValue(CanBeChildProperty);
            set => SetValue(CanBeChildProperty, value);
        }
        public static readonly DependencyProperty CanBeChildProperty =
            DependencyProperty.Register(nameof(CanBeChild), typeof(bool), typeof(TreeViewItemEx));

        [Bindable(true)]
        public ICommand DeleteCommand
        {
            get => (ICommand)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }
        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(TreeViewItemEx));

        [Bindable(true)]
        public ICommand JoinCommand
        {
            get => (ICommand)GetValue(JoinCommandProperty);
            set => SetValue(JoinCommandProperty, value);
        }
        public static readonly DependencyProperty JoinCommandProperty =
            DependencyProperty.Register(nameof(JoinCommand), typeof(ICommand), typeof(TreeViewItemEx));

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ExpandSubtree();
            IsExpanded = false;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if(CanBeChild) dragStartItem = this;
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            SetCurrentValue(EnhancedListViewItem.IsSelectedProperty, true);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if(Mouse.LeftButton != MouseButtonState.Pressed) return;    // 좌클릭 드래그 중이 아니면 return
            if(this != dragStartItem) return;                           // 처음 클릭한 Item이 아니면 return
            if(CanBeChild) DragDrop.DoDragDrop(this, this, DragDropEffects.Move); // Movable Item일 때만 DnD 시작
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            if(CanBeParent) IsSelected = true; // Joinable Item일 경우 선택
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);
            if(CanBeParent) IsSelected = false; // Joinable Item일 경우 선택 해제
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if(CanBeParent && e.Data.GetDataPresent(typeof(TreeViewItemEx))) // DnD 데이터가 랜드마크일 경우
                ((TreeViewItemEx)e.Data.GetData(typeof(TreeViewItemEx))).JoinCommand?.Execute(DataContext); // 그룹 옮김
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if(e.Key == Key.Delete && e.Source is TreeViewItemEx item)
                item.DeleteCommand?.Execute(null);
        }
    }

    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(TreeViewItemEx))]
    public class TreeViewEx : TreeView
    {
        protected override DependencyObject GetContainerForItemOverride() => new TreeViewItemEx();
        protected override bool IsItemItsOwnContainerOverride(object item) => item is TreeViewItemEx;

        [Bindable(true)]
        [Category("Appearance")]
        public new object SelectedItem
        {
            get => GetValue(TreeViewEx.SelectedItemProperty);
            set => SetValue(TreeViewEx.SelectedItemProperty, value);
        }
        public new static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(TreeViewEx),
                new FrameworkPropertyMetadata(OnSelectedItemChanged) { BindsTwoWayByDefault = true });

        protected static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as TreeViewEx;

            //Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) +
            //    $" TreeView Selection changed (Prop): (Pr:{instance.SelectedItem} / Vi:{((TreeView)instance).SelectedItem}) => ({e.NewValue})");

            if(((TreeView)instance).SelectedItem == instance.SelectedItem) return;

            instance.ContainerFromItemExpand(e.NewValue)?.BringIntoView();

            var searchStack = new Stack<TreeViewItemEx>();
            foreach(object curItem in instance.Items)
                if(instance.ItemContainerGenerator.ContainerFromItem(curItem) is TreeViewItemEx curContainer)
                    searchStack.Push(curContainer);

            if(searchStack.Count == 0) return;

            for(var curContainer = searchStack.Pop(); true; curContainer = searchStack.Pop())
            {
                bool isSelected = curContainer.DataContext == e.NewValue;
                if(curContainer.IsSelected != isSelected)
                    curContainer.SetCurrentValue(TreeViewItemEx.IsSelectedProperty, isSelected);
                foreach(object curItem in curContainer.Items)
                    if(curContainer.ItemContainerGenerator.ContainerFromItem(curItem) is TreeViewItemEx childContainer)
                        searchStack.Push(childContainer);
                if(searchStack.Count == 0) break;
            }
        }

        protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            //Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + 
            //    $" TreeView Selection changed (View): (Pr:{SelectedItem} / Vi:{((TreeView)this).SelectedItem}) => ({e.NewValue})");

            //Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + 
            //    $" TreeView Selection changed (View) before propagation : (Pr:{SelectedItem} / Vi:{((TreeView)this).SelectedItem}) => ({e.NewValue})");

            base.OnSelectedItemChanged(e);

            if(SelectedItem != e.NewValue)
            {
                SetCurrentValue(TreeViewEx.SelectedItemProperty, e.NewValue);
                //SelectedItem = e.NewValue;
                //Console.WriteLine("assigned : " + SelectedItem + " => " + e.NewValue);
            }

            //Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
            //SetValue(TreeViewEx.SelectedItemProperty, e.NewValue);//));

            //Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + 
            //    $" TreeView Selection changed (View) after propagation: (Pr:{SelectedItem} / Vi:{((TreeView)this).SelectedItem}) => ({e.NewValue})");
        }

        private TreeViewItemEx ContainerFromItemExpand(object item)
        {
            if(item == null) return null;
            var hierarchyList = new List<ItemsControl>();
            if(TraceItemHierarchyRecurs(item, this, hierarchyList))
            {
                hierarchyList.Reverse();
                foreach(var itemContainer in hierarchyList)
                    if(itemContainer != hierarchyList.Last())
                        (itemContainer as TreeViewItemEx)?.SetCurrentValue(TreeViewItem.IsExpandedProperty, true);
                    else return itemContainer as TreeViewItemEx;
            }

            return null;
        }

        private static bool TraceItemHierarchyRecurs(object targetItem,
            ItemsControl parentContainer, List<ItemsControl> hierarchyList)
        {
            foreach(var curItem in parentContainer.Items)
            {
                var curContainer = (ItemsControl)parentContainer.ItemContainerGenerator.ContainerFromItem(curItem);
                if(curItem == targetItem || TraceItemHierarchyRecurs(targetItem, curContainer, hierarchyList))
                {
                    hierarchyList.Add(curContainer);
                    return true;
                }
            }

            return false;
        }
    }
}
