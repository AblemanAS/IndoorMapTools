using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace IndoorMapTools.View.FGAView
{
    public class FGAPanel : Panel
    {
        // Floor Attached Property
        public static int GetFloor(UIElement target) => (int)target.GetValue(FloorProperty);
        public static void SetFloor(UIElement target, int value) => target.SetValue(FloorProperty, value);
        public static readonly DependencyProperty FloorProperty = DependencyProperty.RegisterAttached("Floor",
            typeof(int), typeof(FGAPanel), new FrameworkPropertyMetadata(-1));

        // Group Attached Property
        public static int GetGroup(UIElement target) => (int)target.GetValue(GroupProperty);
        public static void SetGroup(UIElement target, int value) => target.SetValue(GroupProperty, value);
        public static readonly DependencyProperty GroupProperty = DependencyProperty.RegisterAttached("Group",
            typeof(int), typeof(FGAPanel), new FrameworkPropertyMetadata(-1));

        // Area Attached Property
        public static int GetArea(UIElement target) => (int)target.GetValue(AreaProperty);
        public static void SetArea(UIElement target, int value) => target.SetValue(AreaProperty, value);
        public static readonly DependencyProperty AreaProperty = DependencyProperty.RegisterAttached("Area",
            typeof(int), typeof(FGAPanel), new FrameworkPropertyMetadata(-1));

        private IFGALayoutMapper coordinator;
        private bool reservationValid;

        protected override Size MeasureOverride(Size constraint)
        {
            Size availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
            foreach(UIElement internalChild in InternalChildren)
                internalChild?.Measure(availableSize);

            // 예약된 Cell이 없으면 Measure를 수정하지 않음
            if(reservationValid) return default;

            // Visual Tree를 거슬러 올라가며 coordinator 탐색
            coordinator = FGALayoutHelper.SearchLayoutMapper(this);
            if(coordinator != null)
            {
                coordinator.UpdateReservation(this, Children.Cast<UIElement>().Select(child =>
                    (GetFloor(child), GetGroup(child), GetArea(child))));
                reservationValid = true;
            }

            return default;
        }


        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            if(coordinator == null) // coordinator가 없으면 기본 배치
                foreach(UIElement child in InternalChildren)
                    child.Arrange(new Rect(0, 0, child.DesiredSize.Width, child.DesiredSize.Height));
            else // coordinator가 있으면 예약된 Cell에 배치
                foreach(UIElement child in InternalChildren)
                    child.Arrange(coordinator.GetItemLayoutRect(GetFloor(child), GetGroup(child), GetArea(child)));

            return arrangeBounds;
        }


        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
            reservationValid = false;
            InvalidateMeasure();
            InvalidateArrange();
        }
    }
}
