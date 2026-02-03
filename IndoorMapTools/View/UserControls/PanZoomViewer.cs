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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace IndoorMapTools.View.UserControls
{
    public class PanZoomViewer : Decorator
    {
        private const string GRAB_CURSOR_PATH = "pack://application:,,,/MapView;component/Resources/grab.cur";
        private const string UNGRAB_CURSOR_PATH = "pack://application:,,,/MapView;component/Resources/ungrab.cur";

        static PanZoomViewer()
        {
            GrabCursor = new Cursor(Application.GetResourceStream(new Uri(GRAB_CURSOR_PATH)).Stream);
            UngrabCursor = new Cursor(Application.GetResourceStream(new Uri(UNGRAB_CURSOR_PATH)).Stream);
        }

        [Bindable(true, BindingDirection.OneWay)]
        public Point Center => (Point)GetValue(CenterPropertyKey.DependencyProperty);
        public static readonly DependencyPropertyKey CenterPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(Center), typeof(Point),
                typeof(PanZoomViewer), new FrameworkPropertyMetadata(OnCenterChanged) { AffectsArrange = true });

        private static void OnCenterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as PanZoomViewer).isViewPortValid = false;

        [Bindable(true, BindingDirection.OneWay)]
        public int Zoom => (int)GetValue(ZoomPropertyKey.DependencyProperty);
        public static readonly DependencyPropertyKey ZoomPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(Zoom), typeof(int),
                typeof(PanZoomViewer), new FrameworkPropertyMetadata(OnZoomChanged) { AffectsArrange = true, AffectsMeasure = true });

        private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as PanZoomViewer;
            if(!(instance.Child is UIElement content)) return;
            double scaleFactor = Math.Pow(1.1, instance.Zoom);
            content.RenderTransform = new ScaleTransform(scaleFactor, scaleFactor);
            instance.isFocusableValid = false;
            instance.isViewPortValid = false;
        }

        public static Cursor GrabCursor { get; private set; }
        public static Cursor UngrabCursor { get; private set; }

        private bool isFocusableValid, isViewPortValid;

        public PanZoomViewer()
        {
            ClipToBounds = true;
            SizeChanged += (sender, e) => { isFocusableValid = false; isViewPortValid = false; }; // 핸들러 추가
            Cursor = UngrabCursor; // 렌더링 및 커서 설정
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
            => new PointHitTestResult(this, hitTestParameters.HitPoint);

        protected override Size MeasureOverride(Size constraint)
        {
            Child?.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return default;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if(Child == null) return arrangeSize;
            if(!isFocusableValid) CalculateFocusable();
            if(!isViewPortValid) FixViewPort();

            double scaleFactor = Math.Pow(1.1, Zoom);
            Point loc = new Point(ActualWidth / 2.0 - Center.X * scaleFactor,
                ActualHeight / 2.0 - Center.Y * scaleFactor);
            Child.Arrange(new Rect(loc, Child.DesiredSize));
            return arrangeSize;
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            if(!(visualAdded is UIElement content)) return;

            SetValue(ZoomPropertyKey, 0);

            isFocusableValid = false;
            isViewPortValid = false;

            content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            SetValue(CenterPropertyKey, new Point(content.DesiredSize.Width * 0.5, content.DesiredSize.Height * 0.5));
        }

        /// <summary>
        /// 마우스 휠에 따라 줌 조절
        /// </summary>
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if(Child == null) return;
            SetValue(ZoomPropertyKey, Zoom + (e.Delta > 0 ? 1 : -1));
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if(Child == null) return;

            dragStartPos = e.GetPosition(this);     // 드래그 시작점 뷰 위치 기록
            centerStart = Center;                   // 드래그 시작점 포커스 기록
            CaptureMouse();                         // 마우스 캡쳐 시작
            Cursor = GrabCursor;  // 기본 툴 : 커서 변경
        }


        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if(Child == null) return;

            ReleaseMouseCapture();  // 마우스 캡쳐 (드래그) 종료
            Cursor = UngrabCursor;  // 기본 툴 : 커서 변경
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if(Child == null) return;

            Point curPosition = e.GetPosition(this);

            if(IsMouseCaptured)
            {
                double scaleFactor = Math.Pow(1.1, Zoom);
                double centerX = centerStart.X + (dragStartPos.X - curPosition.X) / scaleFactor;
                double centerY = centerStart.Y + (dragStartPos.Y - curPosition.Y) / scaleFactor;
                SetValue(CenterPropertyKey, new Point(centerX, centerY));
            }
        }


        // 렌더링 파라미터
        private Point centerStart;
        private Point dragStartPos;
        private double focusableLeft;
        private double focusableRight;
        private double focusableTop;
        private double focusableBottom;

        private void CalculateFocusable()
        {
            if(Child == null || ActualWidth == 0 || ActualHeight == 0) return;

            double scaleFactor = Math.Pow(1.1, Zoom);
            double viewPortWidth = ActualWidth / scaleFactor;

            if(viewPortWidth < Child.DesiredSize.Width)
            {
                focusableLeft = viewPortWidth * 0.5;
                focusableRight = Child.DesiredSize.Width - focusableLeft;
            }
            else
            {
                focusableRight = viewPortWidth * 0.5;
                focusableLeft = Child.DesiredSize.Width - focusableRight;
            }

            double viewPortHeight = ActualHeight / scaleFactor;
            if(viewPortHeight < Child.DesiredSize.Height)
            {
                focusableTop = viewPortHeight * 0.5;
                focusableBottom = Child.DesiredSize.Height - focusableTop;
            }
            else
            {
                focusableBottom = viewPortHeight * 0.5;
                focusableTop = Child.DesiredSize.Height - focusableBottom;
            }

            isFocusableValid = true;
        }

        private void FixViewPort()
        {
            if(Child == null || ActualWidth == 0 || ActualHeight == 0) return;

            double centerX = Center.X;
            double centerY = Center.Y;

            bool viewPortFixed = false;
            double overflowPixel;
            overflowPixel = focusableLeft - centerX;
            if(overflowPixel > 0) { centerX += overflowPixel; centerStart.X += overflowPixel; viewPortFixed = true; }
            overflowPixel = centerX - focusableRight;
            if(overflowPixel > 0) { centerX -= overflowPixel; centerStart.X -= overflowPixel; viewPortFixed = true; }
            overflowPixel = focusableTop - centerY;
            if(overflowPixel > 0) { centerY += overflowPixel; centerStart.Y += overflowPixel; viewPortFixed = true; }
            overflowPixel = centerY - focusableBottom;
            if(overflowPixel > 0) { centerY -= overflowPixel; centerStart.Y -= overflowPixel; viewPortFixed = true; }
            if(viewPortFixed) SetValue(CenterPropertyKey, new Point(centerX, centerY));

            isViewPortValid = true;
        }
    }
}
