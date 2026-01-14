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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;


namespace IndoorMapTools.View.UserControls
{
    public class TreeViewItemEx : TreeViewItem
    {
        protected override DependencyObject GetContainerForItemOverride() => new TreeViewItemEx();

        private TreeViewEx owner;

        [Bindable(true)]
        public bool CanBeParent
        {
            get => (bool)GetValue(CanBeParentProperty);
            set => SetValue(CanBeParentProperty, value);
        }
        public static readonly DependencyProperty CanBeParentProperty =
            DependencyProperty.Register(nameof(CanBeParent), typeof(bool), typeof(TreeViewItemEx),
                new FrameworkPropertyMetadata(OnCanBeParentChanged));

        private static void OnCanBeParentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as TreeViewItemEx).AllowDrop = (bool)e.NewValue;

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


        public TreeViewItemEx()
        {
            ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanelEx)));
            VirtualizingStackPanel.SetIsVirtualizing(this, true);
            Loaded += (sender, e) => owner = GetOwnerTreeView();
            Unloaded += (sender, e) => owner = null;
        }


        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if(!CanBeChild) return;
            owner ??= GetOwnerTreeView();
            if(owner == null) return;

            owner.DragStartItem = this;
            owner.DragStartPoint = e.GetPosition(this);
        }


        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if(Mouse.LeftButton != MouseButtonState.Pressed) return;    // 좌클릭 드래그 중이 아니면 return
            if(this != owner.DragStartItem) return;                     // 처음 클릭한 Item이 아니면 return
            if(!CanBeChild) return;

            Point curPos = e.GetPosition(this);
            if(Math.Abs(curPos.X - owner.DragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(curPos.Y - owner.DragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                owner.DragStartItem = this;
                return;
            }

            // 드래그 시작
            var data = new DataObject();
            data.SetData(typeof(TreeViewEx), owner);
            data.SetData(typeof(TreeViewItemEx), this);
            SetCurrentValue(IsSelectedProperty, true); // source selection을 확정

            try { DragDrop.DoDragDrop(this, data, DragDropEffects.Move); }
            finally { EndDrag(); }
        }


        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            if(owner == null) return;
            bool acceptable = CanBeParent && e.Data.GetDataPresent(typeof(TreeViewItemEx)) &&
                ReferenceEquals(e.Data.GetData(typeof(TreeViewEx)), owner);
            e.Effects = acceptable ? DragDropEffects.Move : DragDropEffects.None;
            owner.CurrentDropTarget = acceptable ? this : null;
            e.Handled = true;
        }


        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if(owner == null) return;
            owner.CurrentDropTarget = null;
            if(!CanBeParent) return;
            if(!ReferenceEquals(e.Data.GetData(typeof(TreeViewEx)), owner)) return;
            if(!e.Data.GetDataPresent(typeof(TreeViewItemEx))) return;
            if(e.Data.GetData(typeof(TreeViewItemEx)) is not TreeViewItemEx source || !source.IsLoaded) return;
            source.JoinCommand?.Execute(DataContext);
            e.Handled = true;
        }


        protected override void OnQueryContinueDrag(QueryContinueDragEventArgs e)
        {
            base.OnQueryContinueDrag(e);
            if(owner == null) return;
            if(e.EscapePressed || e.Action == DragAction.Cancel || e.Action == DragAction.Drop)
                EndDrag();
        }


        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);
            SetCurrentValue(IsSelectedProperty, true);
        }


        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if(e.Key == Key.Delete && e.Source is TreeViewItemEx item)
                item.DeleteCommand?.Execute(null);
        }


        private void EndDrag()
        {
            owner.CurrentDropTarget = null;
            owner.DragStartItem = null;
        }


        private TreeViewEx GetOwnerTreeView()
        {
            for(ItemsControl current = this; current != null;
                current = ItemsControlFromItemContainer(current))
            {
                if(current is TreeViewEx treeView) return treeView;
                if(current is not TreeViewItemEx) return null;
            }
            return null;
        }
    }

    /// <summary>
    /// 외부로부터의 SelectedItem 설정을 통해 expand 및 선택을 지원하는 TreeView. 
    /// 기본값으로 TwoWay로 Binding됨. 외부로부터의 SelectedItem 설정에서 기인한 Expand 및 Select 시, 
    /// Hierarchical Search를 하는데, ChildSelector를 설정해 주면 Model 구조를 통해 탐색이 가능. 
    /// TreeViewItem에 대해 CanBeParent, CanBeChild, JoinCommand 설정으로, 
    /// 마우스 드래그 앤 드롭을 통한 Regrouping을 지원. Adorner 지원. 
    /// 선택 후 Delete 키를 통해 DeleteCommand 지원. 
    /// </summary>
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(TreeViewItemEx))]
    public class TreeViewEx : TreeView
    {
        private const int MAX_GETCONTAINER_RETRY = 20;

        protected override DependencyObject GetContainerForItemOverride() => new TreeViewItemEx();
        protected override bool IsItemItsOwnContainerOverride(object item) => item is TreeViewItemEx;

        // 드래그 로직
        internal TreeViewItemEx DragStartItem { get; set; }
        internal Point DragStartPoint { get; set; }
        internal TreeViewItemEx CurrentDropTarget 
        { 
            get => currentDropTarget; 
            set
            {
                if(ReferenceEquals(currentDropTarget, value)) return;

                // 기존 어도너 해제
                if(currentLayer != null && currentAdorner != null)
                    currentLayer.Remove(currentAdorner);
                currentAdorner = null;
                currentLayer = null;
                currentDropTarget = null;

                if(value == null) return;

                var layer = AdornerLayer.GetAdornerLayer(value);
                if(layer == null) return;

                currentLayer = layer;
                currentAdorner = new DropIntoAdorner(value);
                layer.Add(currentAdorner);
                currentDropTarget = value;
            }
        }

        private TreeViewItemEx currentDropTarget;
        private AdornerLayer currentLayer;
        private DropIntoAdorner currentAdorner;

        protected override void OnPreviewDragLeave(DragEventArgs e)
        {
            base.OnPreviewDragLeave(e);
            CurrentDropTarget = null;  // 현재 표시 중인 adorner 제거
        }

        // 외부 선택 로직
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
            if(d is not TreeViewEx instance) return;
            if(((TreeView)instance).SelectedItem == instance.SelectedItem) return;
            _ = SelectItemAsync(instance, e.NewValue);
        }

        private int restGetContainerRetry;

        // 선택된 item 찾아서 Expand, BringIntoView
        private static async Task SelectItemAsync(TreeViewEx instance, object newValue)
        {
            instance.restGetContainerRetry = MAX_GETCONTAINER_RETRY;
            var childSelector = instance.ChildSelector;

            // Search 메서드들을 비동기(Task) 버전으로 구현하거나 내부에서 Yield를 사용
            TreeViewItemEx targetContainer = (childSelector != null) ?
                await SearchContainerInModelTreeAsync(instance, newValue, childSelector) :
                await SearchContainerInViewTreeAsync(instance, newValue);

            if(targetContainer != null)
            {
                targetContainer.BringIntoView();
                targetContainer.IsSelected = true;
            }
        }

        protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            if(ReferenceEquals(e.OldValue, e.NewValue)) return;
            base.OnSelectedItemChanged(e);
            if(SelectedItem != e.NewValue) SetCurrentValue(TreeViewEx.SelectedItemProperty, e.NewValue);
        }

        public TreeViewEx()
        {
            ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanelEx)));
            VirtualizingStackPanel.SetIsVirtualizing(this, true);
        }

        /// <summary>
        /// 어떤 DataContext에 대하여, 그 Children Context들의 Enumerable을 Model 계층구조에서 
        /// 찾아들어가는 함수로, 이것을 제공할 경우 외부 SelectedItem 선택 시 
        /// Expand/BringIntoView 가 O(depth)로 제한되어 매우 효율적으로 동작하게 됨.
        /// Runtime에 매번 평가되므로 매우 안정적인 링크 함수를 제공해야 함. 
        /// <para>이 selector 함수의 반환값 IEnumerable이 null 또는 empty enumerable일 경우, children이 없는 것으로 간주</para>
        /// <para>이 속성값이 null일 경우, UI 컨테이너 기반으로 작동하게 되어 큰 규모의 Tree에서 성능 저하를 유발할 수 있음</para>
        /// </summary>
        public Func<object, IEnumerable> ChildSelector
        {
            get => (Func<object, IEnumerable>)GetValue(ChildSelectorProperty);
            set => SetValue(ChildSelectorProperty, value);
        }
        public static readonly DependencyProperty ChildSelectorProperty = DependencyProperty.Register(nameof(ChildSelector),
                typeof(Func<object, IEnumerable>), typeof(TreeViewEx), new PropertyMetadata(null));


        private static async Task<TreeViewItemEx> SearchContainerInModelTreeAsync(TreeViewEx root, object item, Func<object, IEnumerable> childSelector)
        {
            if(item == null) return null;

            List<object> path = BuildPathByModel(root, item, childSelector) ?? throw new InvalidOperationException(
                    "TreeViewEx.ChildSelector is set, but the target item was not reachable by ChildSelector traversal. " +
                    "ChildSelector must return a complete, acyclic child enumeration consistent with the displayed tree.");

            var container = await ExpandPathAndGetContainer(root, path) ?? throw new InvalidOperationException(
                    "TreeViewEx.ChildSelector is set and the path was found, but item containers could not be generated along the path. " +
                    "Ensure the item hierarchy matches the TreeView ItemsSource and templates.");
            return container;
        }


        private static List<object> BuildPathByModel(TreeViewEx root, object target, Func<object, IEnumerable> childSelector)
        {
            static bool isInPath(List<object> path, object node)
            {
                foreach(var e in path) if(ReferenceEquals(e, node)) return true;
                return false;
            }

            // argument 체크
            if(root == null) throw new ArgumentNullException(nameof(root));
            if(childSelector == null) throw new ArgumentNullException(nameof(childSelector));
            if(target == null) return null;

            // 자료구조 초기화
            List<object> path = new(32);
            Stack<IEnumerator> stack = new(32);

            try
            {
                stack.Push(((IEnumerable)root.Items).GetEnumerator()); // root items 삽입

                // iterative DFS
                while(stack.Count > 0)
                {
                    IEnumerator parentiter = stack.Peek(); // parent 얻고

                    if(!parentiter.MoveNext()) // 현재 parent에 대한 children을 모두 봤는데 없음
                    {
                        (stack.Pop() as IDisposable)?.Dispose(); // 스택 꺼내고
                        if(path.Count > 0) path.RemoveAt(path.Count - 1); // 패스 지우고
                        continue;
                    }

                    object child = parentiter.Current; // child 얻고
                    if(child == null || isInPath(path, child)) continue; // null이거나 loop 생기면 넘김

                    path.Add(child); // 유효한 child일 경우 path에 넣고
                    if(ReferenceEquals(child, target)) return path; // 매치하면 return

                    // 유효한데 match하지 않을 경우 더 깊이 들어가기 위해 다시 enumerator 추출하여 stack에 넣음
                    IEnumerable children = childSelector(child);
                    if(children == null) path.RemoveAt(path.Count - 1);
                    else stack.Push(children.GetEnumerator());

                }

                return null;
            }
            finally { while(stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); }
        }


        private static async Task<TreeViewItemEx> ExpandPathAndGetContainer(TreeViewEx root, List<object> path)
        {
            if(path == null || path.Count == 0) return null;

            ItemsControl parent = root;
            TreeViewItemEx child = null;

            for(int i = 0; i < path.Count; i++)
            {
                object item = path[i];
                var itemsPanel = ExpandAndGetItemsPanel(parent);
                int childIndex = parent.Items.IndexOf(item);
                if(childIndex < 0) return null;
                child = GetChildContainer(parent, itemsPanel, childIndex);
                if(child == null && root.restGetContainerRetry-- > 0)
                {
                    await Dispatcher.Yield(DispatcherPriority.Background); // UI 스레드 제어권을 잠시 넘겨서 WPF 엔진이 객체를 만들게 함
                    child = GetChildContainer(parent, itemsPanel, childIndex); // 재시도
                }
                if(child == null) return null;
                parent = child;
            }

            return child;
        }


        private static async Task<TreeViewItemEx> SearchContainerInViewTreeAsync(TreeViewEx root, object item)
        {
            if(root == null) return null;

            // 자료구조 초기화
            Stack<(ItemsControl, bool origExpanded, Panel, IEnumerator<int>)> stack = new(32);
            stack.Push((root, false, ExpandAndGetItemsPanel(root), Enumerable.Range(0, root.Items.Count).GetEnumerator()));

            while(stack.Count > 0)
            {
                (ItemsControl parent, bool origExpanded, Panel itemsPanel, IEnumerator<int> iter) = stack.Peek();

                if(!iter.MoveNext()) // 현재 parent에 대한 children을 모두 봤는데 없음
                {
                    stack.Pop(); // 스택 꺼내고
                    if(parent is TreeViewItem parentItem) parentItem.IsExpanded = origExpanded;
                    continue;
                }

                int childIndex = iter.Current; // child 얻고

                var child = GetChildContainer(parent, itemsPanel, childIndex);// 컨테이너가 null이면 (가상화 등에 의해 아직 안 만들어졌다면)
                if(child == null && root.restGetContainerRetry-- > 0)
                {
                    await Dispatcher.Yield(DispatcherPriority.Background); // UI 스레드 제어권을 잠시 넘겨서 WPF 엔진이 객체를 만들게 함
                    child = GetChildContainer(parent, itemsPanel, childIndex); // 재시도
                }

                if(child == null) continue;
                if(ReferenceEquals(child.DataContext, item)) return child; // 매치하면 return

                // 유효한데 match하지 않을 경우 더 깊이 들어가기 위해 다시 stack에 넣음
                stack.Push((child, child.IsExpanded, ExpandAndGetItemsPanel(child), Enumerable.Range(0, child.Items.Count).GetEnumerator()));

            }

            return null;
        }


        private static Panel ExpandAndGetItemsPanel(ItemsControl parent)
        {
            if(parent is TreeViewItem item && !item.IsExpanded)
                parent.SetValue(TreeViewItem.IsExpandedProperty, true);
            parent.ApplyTemplate();
            var itemsPresenter = (ItemsPresenter)parent.Template.FindName("ItemsHost", parent);

            // ItemsPresenter 받아서 템플릿 적용 -> 컨테이너 강제생성
            if(itemsPresenter != null) itemsPresenter.ApplyTemplate();
            else
            {
                itemsPresenter = FindVisualChild<ItemsPresenter>(parent);
                if(itemsPresenter == null)
                {
                    parent.UpdateLayout();
                    itemsPresenter = FindVisualChild<ItemsPresenter>(parent);
                }
            }

            // children 강제생성
            var itemsHostPanel = (Panel)VisualTreeHelper.GetChild(itemsPresenter, 0);
            _ = itemsHostPanel.Children;

            return itemsHostPanel;
        }


        private static TreeViewItemEx GetChildContainer(ItemsControl parent, Panel itemsPanel, int index)
        {
            TreeViewItemEx child;
            if(itemsPanel is VirtualizingStackPanelEx virtualizingPanel) // 현재 컨테이너가 가상화 적용일 경우
            {
                virtualizingPanel.BringIntoView(index); // 먼저 view로 가져온 뒤
                child = (TreeViewItemEx)parent.ItemContainerGenerator.ContainerFromIndex(index); // 컨테이너 얻기
            }
            else
            {
                child = (TreeViewItemEx)parent.ItemContainerGenerator.ContainerFromIndex(index); // 컨테이너 얻은 뒤
                child?.BringIntoView(); // 가상화가 아니더라도 view로 가져와서 점차 viewport 옮기기
            }

            return child;
        }


        private static T FindVisualChild<T>(Visual visual) where T : Visual
        {
            Queue<DependencyObject> queue = new();
            queue.Enqueue(visual);

            while(queue.Count > 0)
            {
                var parent = queue.Dequeue();

                for(int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    Visual child = (Visual)VisualTreeHelper.GetChild(parent, i);
                    if(child is T correctlyTyped) return correctlyTyped;
                    queue.Enqueue(child);
                }
            }

            return null;
        }


        private sealed class DropIntoAdorner : Adorner
        {
            public DropIntoAdorner(UIElement adornedElement) : base(adornedElement)
                => IsHitTestVisible = false;

            protected override void OnRender(DrawingContext dc)
            {
                base.OnRender(dc);
                var size = AdornedElement.RenderSize;
                if(size.Width <= 0 || size.Height <= 0) return;
                var fill = SystemColors.HighlightBrush.Clone();
                fill.Opacity = 0.10;
                dc.DrawRectangle(fill, null, new Rect(0, 0, size.Width, size.Height));
            }
        }
    }

    internal sealed class VirtualizingStackPanelEx : VirtualizingStackPanel
    { public void BringIntoView(int index) => BringIndexIntoView(index); }
}