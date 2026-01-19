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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FGAView.System.Windows.Controls
{
    /// <summary>
    /// Floor, Group, Area가 자연수로 resolve되는 Children을, FGA Layout에 따라 배치. 
    /// Floor, Group, Area가 유효하지 않은 Children의 경우 일반적인 Canvas처럼 배치. 
    /// 매 Measure마다 가장 가까운 IFGALayoutMapper를 찾아 레이아웃 조율을 받음. 
    /// 시각 트리 직계 부모에서 IFGALayoutMapper를 찾지 못하면 일반적인 Canvas처럼 동작. 
    /// </summary>
    public class FGACanvas : Canvas
    {
        // Floor Attached Property
        public static int GetFloor(UIElement target) => (int)target.GetValue(FloorProperty);
        public static void SetFloor(UIElement target, int value) => target.SetValue(FloorProperty, value);
        public static readonly DependencyProperty FloorProperty = DependencyProperty.RegisterAttached("Floor",
            typeof(int), typeof(FGACanvas), new FrameworkPropertyMetadata(-1, OnFGALayoutDataChanged));

        // Group Attached Property
        public static int GetGroup(UIElement target) => (int)target.GetValue(GroupProperty);
        public static void SetGroup(UIElement target, int value) => target.SetValue(GroupProperty, value);
        public static readonly DependencyProperty GroupProperty = DependencyProperty.RegisterAttached("Group",
            typeof(int), typeof(FGACanvas), new FrameworkPropertyMetadata(-1, OnFGALayoutDataChanged));

        // Area Attached Property
        public static int GetArea(UIElement target) => (int)target.GetValue(AreaProperty);
        public static void SetArea(UIElement target, int value) => target.SetValue(AreaProperty, value);
        public static readonly DependencyProperty AreaProperty = DependencyProperty.RegisterAttached("Area",
            typeof(int), typeof(FGACanvas), new FrameworkPropertyMetadata(-1, OnFGALayoutDataChanged));

        private static void OnFGALayoutDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is UIElement target && VisualTreeHelper.GetParent(target) is FGACanvas instance)) return;
            instance.reservationValid = false;
            instance.InvalidateMeasure(); // FGA값은 Layout에만 관련있으나, 예약 갱신을 위해 Measure까지 호출
        }


        private IFGALayoutMapper mapper;
        private bool reservationValid = false;


        protected override Size MeasureOverride(Size constraint)
        {
            Size availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
            foreach(UIElement internalChild in InternalChildren)
                internalChild?.Measure(availableSize);

            // 예약사항이 바뀐게 없으면 예약 요청을 수정하지 않음
            if(reservationValid) return default;

            // Visual Tree를 거슬러 올라가며 매퍼 탐색
            mapper = FGALayoutHelper.SearchLayoutMapper(this);
            if(mapper != null)
            {
                // 매퍼를 찾을 경우 해당 매퍼에 FGA 자리 예약 요청
                mapper.UpdateReservation(this, Children.Cast<UIElement>().Select(child =>
                    (GetFloor(child), GetGroup(child), GetArea(child))));
                reservationValid = true;
            }

            return default;
        }


        protected override Size ArrangeOverride(Size arrangeSize)
        {
            // 매퍼가 없으면 기본 캔버스식 배치
            if(mapper == null) return base.ArrangeOverride(arrangeSize);

            // 매퍼가 있으면 매퍼의 지시에 따라 배치
            foreach(UIElement child in InternalChildren)
            {
                int childF = GetFloor(child);
                int childG = GetGroup(child);
                int childA = GetArea(child);

                // invalid FGA value -> Canvas식 배치
                if(childF < 0 || childG < 0 || childA < 0) CanvasArrange(arrangeSize, child);
                else child.Arrange(mapper.GetItemLayoutRect(GetFloor(child), GetGroup(child), GetArea(child)));
            }

            return arrangeSize;
        }


        // Canvas 식 배치 (Canvas 기본 구현과 동일 behavior)
        private void CanvasArrange(Size arrangeSize, UIElement child)
        {
            double x = 0.0;
            double y = 0.0;
            double left = GetLeft(child);
            if(!double.IsNaN(left)) x = left;
            else
            {
                double right = GetRight(child);
                if(!double.IsNaN(right)) x = arrangeSize.Width - child.DesiredSize.Width - right;
            }

            double top = GetTop(child);
            if(!double.IsNaN(top)) y = top;
            else
            {
                double bottom = GetBottom(child);
                if(!double.IsNaN(bottom)) y = arrangeSize.Height - child.DesiredSize.Height - bottom;
            }

            child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
        }


        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
            reservationValid = false;
            InvalidateMeasure();
        }
    }
}
