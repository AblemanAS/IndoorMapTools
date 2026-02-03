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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapView.Controls
{
    [ContentProperty(nameof(MapElements))]
    public class MapView : Panel
    {
        private const string GRAB_CURSOR_PATH = "pack://application:,,,/MapView;component/Resources/grab.cur";
        private const string UNGRAB_CURSOR_PATH = "pack://application:,,,/MapView;component/Resources/ungrab.cur";

        static MapView()
        {
            GrabCursor = new Cursor(Application.GetResourceStream(new Uri(GRAB_CURSOR_PATH)).Stream, true);
            UngrabCursor = new Cursor(Application.GetResourceStream(new Uri(UNGRAB_CURSOR_PATH)).Stream, true);
        }

        /// <summary> MapView 캔버스 내 뷰포트 중앙 점 (픽셀 위치) </summary>
        [Bindable(true)]
        public Point Center
        {
            get => (Point)GetValue(CenterProperty);
            set => SetValue(CenterProperty, value);
        }
        public static readonly DependencyProperty CenterProperty = DependencyProperty.Register(nameof(Center), 
            typeof(Point), typeof(MapView), new FrameworkPropertyMetadata(OnCenterChanged) { AffectsArrange = true });

        private static void OnCenterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is MapView instance)) return;
            instance.isViewPortValid = false;
        }

        /// <summary> MapView 캔버스 내 뷰포트 확대/축소 </summary>
        [Bindable(true)]
        public int Zoom
        {
            get => (int)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }
        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(nameof(Zoom), typeof(int), typeof(MapView),
                new FrameworkPropertyMetadata(OnZoomChanged));

        private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as MapView).SetValue(ImageScaleFactorPropertyKey, Math.Pow(1.1, (int)e.NewValue));

        /// <summary> Zoom에 따라, 실제로 렌더링 스케일에 영향을 주는 읽기전용 DP </summary>
        public double ImageScaleFactor => (double)GetValue(ImageScaleFactorPropertyKey.DependencyProperty);
        public static readonly DependencyPropertyKey ImageScaleFactorPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(ImageScaleFactor), typeof(double), typeof(MapView), 
                new FrameworkPropertyMetadata(1.0, OnImageScaleFactorChanged) { AffectsArrange = true });

        private static void OnImageScaleFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is MapView instance && e.NewValue is double value)) return;
            instance.overlayScaleTransform.ScaleX = value;
            instance.overlayScaleTransform.ScaleY = value;
            instance.isFocusableValid = false;
            instance.isViewPortValid = false;
        }

        // 맵 에디터 툴
        [Bindable(true)]
        public MouseTool ActiveTool
        {
            get => (MouseTool)GetValue(ActiveToolProperty);
            set => SetValue(ActiveToolProperty, value);
        }
        public static readonly DependencyProperty ActiveToolProperty =
            DependencyProperty.Register(nameof(ActiveTool), typeof(MouseTool), typeof(MapView),
                new FrameworkPropertyMetadata(OnActiveToolChanged) { BindsTwoWayByDefault = true });

        private static void OnActiveToolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is MapView instance)) return;
            instance.Cursor = e.NewValue is MouseTool newTool ? newTool.DefaultCursor : UngrabCursor;
        }

        // 렌더링 소스
        [Bindable(true)]
        public BitmapSource MapImageSource
        {
            get => (BitmapSource)GetValue(MapImageSourceProperty);
            set => SetValue(MapImageSourceProperty, value);
        }
        public static readonly DependencyProperty MapImageSourceProperty = DependencyProperty.Register(nameof(MapImageSource),
            typeof(BitmapSource), typeof(MapView), new FrameworkPropertyMetadata(OnMapImageSourceChanged));

        private static void OnMapImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is MapView instance)) return;

            //instance.overlayCanvas.SetBackgroundSource(newSource);
            if(e.NewValue is BitmapSource newSource)
            {
                var bgBrush = new ImageBrush(newSource) { Stretch = Stretch.None };
                bgBrush.Freeze();
                instance.overlayCanvas.Background = bgBrush;
                instance.overlayCanvas.Width = newSource.PixelWidth;
                instance.overlayCanvas.Height = newSource.PixelHeight;
            }
            else instance.overlayCanvas.Background = null;

            instance.SetCurrentValue(ZoomProperty, 0);
            instance.SetCurrentValue(ActiveToolProperty, null);
            instance.isFocusableValid = false;
            instance.isViewPortValid = false;
        }

        public UIElementCollection MapElements => elementCanvas.Children;
        public UIElementCollection MapOverlays => overlayCanvas.Children;
        public UIElementCollection KeyActivationTools { get; }
        public static Cursor GrabCursor { get; private set; }
        public static Cursor UngrabCursor { get; private set; }

        private readonly DragBoxVisual dragBox = new DragBoxVisual();
        private readonly Canvas overlayCanvas = new Canvas();
        private readonly Canvas elementCanvas = new Canvas();
        private readonly ScaleTransform overlayScaleTransform = new ScaleTransform(1.0, 1.0);
        private bool isFocusableValid, isViewPortValid;
        private bool subscribingInput;

        public MapView()
        {
            Background = Brushes.White;
            ClipToBounds = true;

            // 툴 컨테이너 초기화
            var toolGroup = new Canvas { Visibility = Visibility.Collapsed, 
                IsHitTestVisible = false, Focusable = false };
            KeyActivationTools = toolGroup.Children;

            // 오버레이 캔버스 scale 설정
            overlayCanvas.RenderTransform = overlayScaleTransform;
            overlayCanvas.ClipToBounds = false;

            // 메인 캔버스 및 기타 시각요소 초기화
            Children.Add(toolGroup);
            Children.Add(overlayCanvas);
            Children.Add(elementCanvas);
            Children.Add(dragBox);

            // 핸들러 추가
            SizeChanged += (sender, e) => { isFocusableValid = false; isViewPortValid = false; };
            
            // 렌더링 및 커서 설정
            Cursor = UngrabCursor;

            Loaded += (sender, e) => 
            {
                if(subscribingInput) return;
                subscribingInput = true;
                InputManager.Current.PreProcessInput += onPreProcessInput;
            };

            Unloaded += (sender, e) =>
            {
                if(!subscribingInput) return;
                subscribingInput = false;
                InputManager.Current.PreProcessInput -= onPreProcessInput;
            };

            void onPreProcessInput(object sender, PreProcessInputEventArgs e)
            { if(IsVisible && IsEnabled && e.StagingItem.Input is KeyEventArgs) UpdateTool(); }
        }


        protected override Size MeasureOverride(Size constraint)
        {
            Size availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
            overlayCanvas.Measure(availableSize);
            elementCanvas.Measure(availableSize);
            dragBox.Measure(availableSize);
            return default;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if(!isFocusableValid) CalculateFocusableRect();
            if(!isViewPortValid) FixViewPort();

            Point loc = new Point(ActualWidth / 2.0 - Center.X * ImageScaleFactor, 
                ActualHeight / 2.0 - Center.Y * ImageScaleFactor);
            overlayCanvas.Arrange(new Rect(loc, overlayCanvas.DesiredSize));
            elementCanvas.Arrange(new Rect(loc, elementCanvas.DesiredSize));
            dragBox.Arrange(new Rect(new Point(0, 0), dragBox.DesiredSize));

            return arrangeSize;
        }

        /// <summary>
        /// 마우스 휠에 따라 줌 조절
        /// </summary>
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if(MapImageSource == null) return;
            SetCurrentValue(ZoomProperty, Zoom + (e.Delta > 0 ? 1 : -1));
        }


        private MouseToolEventArgs GenerateMouseToolEvent(MouseEventArgs e) => new MouseToolEventArgs
        {
            Position = CvtCoordViewToImage(e.GetPosition(this)),
            DragBox = dragBoxOnImage // DragBoxEnabled 일 때만 제대로 된 값이 계산되어 주어짐, 이외의 경우에는 쓰레기 값
        };


        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if(MapImageSource == null) return;

            switch(e.ChangedButton)
            {
                case MouseButton.Left:
                    dragStartPos = e.GetPosition(this);     // 드래그 시작점 뷰 위치 기록
                    centerStart = Center;                   // 드래그 시작점 포커스 기록
                    CaptureMouse();                         // 마우스 캡쳐 시작

                    if(ActiveTool == null) Cursor = GrabCursor;  // 기본 툴 : 커서 변경
                    else // 기타 툴 : 클릭 커서 있으면 커서 변경 후, MouseTool LB Down 핸들러 호출
                    {
                        if(ActiveTool.ClickedCursor != null) Cursor = ActiveTool.ClickedCursor;
                        ActiveTool.OnMouseLeftDown?.Execute(GenerateMouseToolEvent(e));
                    }
                    break;

                case MouseButton.Right:
                    if(ActiveTool == null) return;

                    // 우클릭 액션이 있으면 실행
                    if(ActiveTool.OnMouseRightDown != null)
                        ActiveTool.OnMouseRightDown.Execute(GenerateMouseToolEvent(e));
                    // 우클릭 액션이 없고 버튼 트리거 툴일 경우 툴 해제 (드래그 중이면 무시)
                    else if(ActiveTool.ActivationKey == Key.None && !IsMouseCaptured)
                        SetCurrentValue(ActiveToolProperty, null);

                    break;

                case MouseButton.XButton1: // Back button
                    ActiveTool?.OnMouseXButton1Down?.Execute(GenerateMouseToolEvent(e));
                    break;

                case MouseButton.XButton2: // Forward button
                    ActiveTool?.OnMouseXButton2Down?.Execute(GenerateMouseToolEvent(e));
                    break;
            }
        }


        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if(MapImageSource == null) return;

            switch(e.ChangedButton)
            {
                case MouseButton.Left:
                    ActiveTool?.OnMouseLeftUp?.Execute(GenerateMouseToolEvent(e)); // MouseTool LB Up 이벤트 호출

                    ReleaseMouseCapture();  // 마우스 캡쳐 (드래그) 종료
                    if(ActiveTool == null) Cursor = UngrabCursor;  // 기본 툴 : 커서 변경
                    else // 기타 툴
                    {
                        if(ActiveTool.ClickedCursor != null) 
                            Cursor = ActiveTool.DefaultCursor; // 설정된 클릭 커서가 있을 경우 커서 변경
                        if(dragBox.Visibility == Visibility.Visible)
                            dragBox.Visibility = Visibility.Hidden; // 드래그박스가 켜져 있을 경우 드래그박스 해제
                    }
                    break;

                case MouseButton.Right:
                    ActiveTool?.OnMouseRightUp?.Execute(GenerateMouseToolEvent(e)); // MouseTool RB Up 이벤트 호출
                    break;

                case MouseButton.XButton1: // Back button
                    ActiveTool?.OnMouseXButton1Up?.Execute(GenerateMouseToolEvent(e)); // MouseTool XButton1 Up 이벤트 호출
                    break;

                case MouseButton.XButton2: // Forward button
                    ActiveTool?.OnMouseXButton2Up?.Execute(GenerateMouseToolEvent(e)); // MouseTool XButton2 Up 이벤트 호출
                    break;
            }
        }


        protected override void OnMouseMove(MouseEventArgs e)
        {
            if(MapImageSource == null) return;

            Point curPosition = e.GetPosition(this);

            UpdateTool();

            // 기본 툴 : 포커스 조정
            if(ActiveTool == null)
            {
                if(IsMouseCaptured)
                {
                    double centerX = centerStart.X + (dragStartPos.X - curPosition.X) / ImageScaleFactor;
                    double centerY = centerStart.Y + (dragStartPos.Y - curPosition.Y) / ImageScaleFactor;
                    SetCurrentValue(CenterProperty, new Point(centerX, centerY));
                }
                return;
            }

            // MouseTool Move 핸들러 호출
            ActiveTool.OnMouseMove?.Execute(GenerateMouseToolEvent(e));

            if(IsMouseCaptured) // 드래그 중이면
            {
                // DragBoxEnabled 일 경우 DragBox 계산
                if(ActiveTool.DragBoxEnabled)
                {
                    dragBoxOnView.X = Math.Min(dragStartPos.X, curPosition.X);
                    dragBoxOnView.Y = Math.Min(dragStartPos.Y, curPosition.Y);
                    dragBoxOnView.Width = Math.Abs(dragStartPos.X - curPosition.X);
                    dragBoxOnView.Height = Math.Abs(dragStartPos.Y - curPosition.Y);

                    dragBox.Visibility = Visibility.Visible;
                    dragBox.UpdateVisual(dragBoxOnView);
                    dragBoxOnImage = new Rect(CvtCoordViewToImage(dragBoxOnView.TopLeft), CvtCoordViewToImage(dragBoxOnView.BottomRight));
                }

                // MouseTool LB Drag 핸들러 호출
                ActiveTool.OnMouseLeftDrag?.Execute(GenerateMouseToolEvent(e));
            }
        }


        // 렌더링 파라미터
        private Point centerStart;
        private Point dragStartPos;
        private Rect dragBoxOnView;
        private Rect dragBoxOnImage;
        private double focusableLeft;
        private double focusableRight;
        private double focusableTop;
        private double focusableBottom;

        //private Point CvtCoordViewToImage(Point coordinate) => TranslatePoint(coordinate, overlayCanvas);
        private Point CvtCoordViewToImage(Point coordinate)
        {
            double s = ImageScaleFactor;
            double originX = ActualWidth * 0.5 - Center.X * s;
            double originY = ActualHeight * 0.5 - Center.Y * s;
            return new Point((coordinate.X - originX) / s, (coordinate.Y - originY) / s);
        }


        private void CalculateFocusableRect()
        {
            if(MapImageSource == null || ActualWidth < 1e-6 || ActualHeight < 1e-6) return;

            double viewPortWidth = ActualWidth / ImageScaleFactor;
            if(viewPortWidth < MapImageSource.PixelWidth)
            {
                focusableLeft = viewPortWidth * 0.5;
                focusableRight = MapImageSource.PixelWidth - focusableLeft;
            }
            else
            {
                focusableRight = viewPortWidth * 0.5;
                focusableLeft = MapImageSource.PixelWidth - focusableRight;
            }

            double viewPortHeight = ActualHeight / ImageScaleFactor;
            if(viewPortHeight < MapImageSource.PixelHeight)
            {
                focusableTop = viewPortHeight * 0.5;
                focusableBottom = MapImageSource.PixelHeight - focusableTop;
            }
            else
            {
                focusableBottom = viewPortHeight * 0.5;
                focusableTop = MapImageSource.PixelHeight - focusableBottom;
            }

            isFocusableValid = true;
        }

        private void FixViewPort()
        {
            if(MapImageSource == null || ActualWidth == 0 || ActualHeight == 0) return;

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
            if(viewPortFixed) SetCurrentValue(CenterProperty, new Point(centerX, centerY));

            isViewPortValid = true;
        }

        private void UpdateTool()
        {
            // 맵이 없거나 드래그 중이면 나감
            if(MapImageSource == null || IsMouseCaptured) return;

            if(ActiveTool == null) // 현재 툴이 없을 경우
            {
                foreach(MouseTool tool in KeyActivationTools)
                {
                    if(Keyboard.IsKeyDown(tool.ActivationKey))
                    {
                        SetCurrentValue(ActiveToolProperty, tool);
                        return;
                    }
                }
            }
            else // 현재 툴이 있을 경우
            {
                if(ActiveTool.ActivationKey != Key.None) // 키 트리거 툴일 경우
                {
                    if(!Keyboard.IsKeyDown(ActiveTool.ActivationKey)) // 트리거 키가 안 눌려 있을 경우
                        ReleaseTool();
                }
                else if(Keyboard.IsKeyDown(Key.Escape)) // 토글 트리거 툴이면서 ESC가 눌린 경우
                    ReleaseTool();
            }
        }

        private void ReleaseTool()
        {
            ReleaseMouseCapture();
            SetCurrentValue(ActiveToolProperty, null);

            if(dragBox.Visibility == Visibility.Visible)
                dragBox.Visibility = Visibility.Hidden; // 드래그박스가 켜져 있을 경우 드래그박스 해제
        }

        private class DragBoxVisual : UIElement
        {
            static DragBoxVisual() => STROKE_STYLE.Freeze();
            private static readonly Pen STROKE_STYLE = new Pen(Brushes.Black, 1) { DashStyle = DashStyles.Dash };

            private Rect boxRect;
            public DragBoxVisual() { IsHitTestVisible = false; Visibility = Visibility.Hidden; }
            public void UpdateVisual(Rect boxRect) { this.boxRect = boxRect; InvalidateVisual(); }
            protected override void OnRender(DrawingContext dc) => dc.DrawRectangle(null, STROKE_STYLE, boxRect);
        }
    }
}
