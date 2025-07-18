using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace MapView.System.Windows.Controls
{
    [ContentProperty(nameof(MapElements))]
    public class Map : Panel
    {
        private const string GRAB_CURSOR_PATH = "pack://application:,,,/MapView;component/Resources/grab.cur";
        private const string UNGRAB_CURSOR_PATH = "pack://application:,,,/MapView;component/Resources/ungrab.cur";

        static Map()
        {
            GrabCursor = new Cursor(Application.GetResourceStream(new Uri(GRAB_CURSOR_PATH)).Stream);
            UngrabCursor = new Cursor(Application.GetResourceStream(new Uri(UNGRAB_CURSOR_PATH)).Stream);
        }

        public static bool GetScaledOnZoom(DependencyObject obj) => (bool)obj.GetValue(ScaledOnZoomProperty);
        public static void SetScaledOnZoom(DependencyObject obj, bool value) => obj.SetValue(ScaledOnZoomProperty, value);
        public static readonly DependencyProperty ScaledOnZoomProperty = DependencyProperty.RegisterAttached("ScaledOnZoom",
                typeof(bool), typeof(Map), new PropertyMetadata(false, OnScaledOnZoomChanged));
        private static void OnScaledOnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is DependencyObject reference && VisualTreeHelper.GetParent(reference) is MapCanvas canvas)
                canvas.InvalidateMeasure();
        }

        /// <summary> MapView 캔버스 내 뷰포트 중앙 점 (픽셀 위치) </summary>
        [Bindable(true)]
        public Point Center
        {
            get => (Point)GetValue(CenterProperty);
            set => SetValue(CenterProperty, value);
        }
        public static readonly DependencyProperty CenterProperty = DependencyProperty.Register(nameof(Center), 
            typeof(Point), typeof(Map), new FrameworkPropertyMetadata(OnCenterChanged) { AffectsArrange = true });

        private static void OnCenterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as Map).isViewPortValid = false;

        /// <summary> MapView 캔버스 내 뷰포트 확대/축소 </summary>
        [Bindable(true)]
        public int Zoom
        {
            get => (int)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }
        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(nameof(Zoom), typeof(int), typeof(Map),
                new FrameworkPropertyMetadata(OnZoomChanged));

        private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as Map).SetValue(ImageScaleFactorPropertyKey, Math.Pow(1.1, (int)e.NewValue));

        public double ImageScaleFactor => (double)GetValue(ImageScaleFactorPropertyKey.DependencyProperty);
        public static readonly DependencyPropertyKey ImageScaleFactorPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(ImageScaleFactor), typeof(double),
                typeof(Map), new FrameworkPropertyMetadata(1.0, OnImageScaleFactorChanged) 
                { AffectsMeasure = true, AffectsArrange = true, AffectsRender = true });

        private static void OnImageScaleFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as Map;
            instance.childrenCanvas.SetValue(MapCanvas.ScaleProperty, e.NewValue);
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
            DependencyProperty.Register(nameof(ActiveTool), typeof(MouseTool), typeof(Map),
                new FrameworkPropertyMetadata(OnActiveToolChanged) { BindsTwoWayByDefault = true });

        private static void OnActiveToolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as Map;

            if(e.NewValue is MouseTool newTool)
                instance.Cursor = newTool.DefaultCursor;
            else instance.Cursor = UngrabCursor;
        }

        // 렌더링 소스
        [Bindable(true)]
        public ImageSource MapImageSource
        {
            get => (ImageSource)GetValue(MapImageSourceProperty);
            set => SetValue(MapImageSourceProperty, value);
        }
        public static readonly DependencyProperty MapImageSourceProperty = DependencyProperty.Register(nameof(MapImageSource),
            typeof(ImageSource), typeof(Map), new FrameworkPropertyMetadata(OnMapImageSourceChanged));

        private static void OnMapImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as Map;
            var newSource = e.NewValue as ImageSource;

            instance.childrenCanvas.SetValue(MapCanvas.BackgroundSourceProperty, newSource);
            instance.SetValue(ZoomProperty, 0);
            instance.SetValue(ActiveToolProperty, null);
            instance.isFocusableValid = false;
            instance.isViewPortValid = false;

            if(newSource == null) return;
            instance.SetValue(CenterProperty, new Point(newSource.Width * 0.5, newSource.Height * 0.5));
        }

        public UIElementCollection MapElements => childrenCanvas.Children;
        public UIElementCollection KeyActivationTools { get; }
        public static Cursor GrabCursor { get; private set; }
        public static Cursor UngrabCursor { get; private set; }

        private readonly DragBoxVisual dragBox = new DragBoxVisual();
        private readonly MapCanvas childrenCanvas = new MapCanvas();

        private bool isFocusableValid, isViewPortValid;

        public Map()
        {
            Background = Brushes.White;
            ClipToBounds = true;

            // 메인 캔버스 및 기타 시각요소 초기화
            Children.Add(childrenCanvas);
            Children.Add(dragBox);

            // 툴 컨테이너 초기화
            var toolGroup = new Canvas();
            toolGroup.SetBinding(DataContextProperty, new Binding(nameof(DataContext)) { Source = this });
            KeyActivationTools = toolGroup.Children;

            // 핸들러 추가
            SizeChanged += (sender, e) => { isFocusableValid = false; isViewPortValid = false; };
            InputManager.Current.PreProcessInput += (sender, e) => { if(IsVisible && e.StagingItem.Input is KeyEventArgs) UpdateTool(); };
            
            // 렌더링 및 커서 설정
            Cursor = UngrabCursor;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Size availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
            childrenCanvas.Measure(availableSize);
            dragBox.Measure(availableSize);
            return default;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if(!isFocusableValid) CalculateFocusable();
            if(!isViewPortValid) FixViewPort();

            Point loc = new Point(ActualWidth / 2.0 - Center.X * ImageScaleFactor, 
                ActualHeight / 2.0 - Center.Y * ImageScaleFactor);
            childrenCanvas.Arrange(new Rect(loc, childrenCanvas.DesiredSize));
            dragBox.Arrange(new Rect(new Point(0, 0), dragBox.DesiredSize));

            return arrangeSize;
        }

        /// <summary>
        /// 마우스 휠에 따라 줌 조절
        /// </summary>
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if(MapImageSource == null) return;
            SetValue(ZoomProperty, Zoom + (e.Delta > 0 ? 1 : -1));
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
                        SetValue(ActiveToolProperty, null);

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
                    SetValue(CenterProperty, new Point(centerX, centerY));
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

        private Point CvtCoordViewToImage(Point coordinate)
        {
            Point pointInControl = TranslatePoint(coordinate, childrenCanvas);
            return new Point(pointInControl.X / ImageScaleFactor, pointInControl.Y / ImageScaleFactor);
        }


        private void CalculateFocusable()
        {
            if(MapImageSource == null || ActualWidth == 0 || ActualHeight == 0) return;

            double viewPortWidth = ActualWidth / ImageScaleFactor;
            if(viewPortWidth < MapImageSource.Width)
            {
                focusableLeft = viewPortWidth * 0.5;
                focusableRight = MapImageSource.Width - focusableLeft;
            }
            else
            {
                focusableRight = viewPortWidth * 0.5;
                focusableLeft = MapImageSource.Width - focusableRight;
            }

            double viewPortHeight = ActualHeight / ImageScaleFactor;
            if(viewPortHeight < MapImageSource.Height)
            {
                focusableTop = viewPortHeight * 0.5;
                focusableBottom = MapImageSource.Height - focusableTop;
            }
            else
            {
                focusableBottom = viewPortHeight * 0.5;
                focusableTop = MapImageSource.Height - focusableBottom;
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
            if(viewPortFixed) SetValue(CenterProperty, new Point(centerX, centerY));

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
                        SetValue(ActiveToolProperty, tool);
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
            SetValue(ActiveToolProperty, null);

            if(dragBox.Visibility == Visibility.Visible)
                dragBox.Visibility = Visibility.Hidden; // 드래그박스가 켜져 있을 경우 드래그박스 해제
        }

        private class MapCanvas : Canvas
        {
            [Bindable(true)]
            public double Scale
            {
                get => (double)GetValue(ScaleProperty);
                set => SetValue(ScaleProperty, value);
            }
            public static readonly DependencyProperty ScaleProperty = 
                DependencyProperty.Register(nameof(Scale), typeof(double), typeof(MapCanvas),
                new FrameworkPropertyMetadata(1.0) { AffectsRender = true, AffectsMeasure = true });

            [Bindable(true)]
            public ImageSource BackgroundSource
            {
                get => (ImageSource)GetValue(BackgroundSourceProperty);
                set => SetValue(BackgroundSourceProperty, value);
            }
            public static readonly DependencyProperty BackgroundSourceProperty = 
                DependencyProperty.Register(nameof(BackgroundSource), typeof(ImageSource), typeof(MapCanvas), 
                new FrameworkPropertyMetadata { AffectsRender = true, AffectsMeasure = true });

            protected override void OnRender(DrawingContext dc)
            {
                if(BackgroundSource == null) return;
                dc.DrawImage(BackgroundSource, new Rect(0.0, 0.0, BackgroundSource.Width * Scale, BackgroundSource.Height * Scale));
            }

            protected override Size MeasureOverride(Size constraint)
            {
                Size availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
                foreach(UIElement internalChild in InternalChildren)
                {
                    if(internalChild == null) continue;
                    internalChild.SetCurrentValue(FrameworkElement.LayoutTransformProperty,
                        GetScaledOnZoom(internalChild) ? new ScaleTransform(Scale, Scale) : null);
                    internalChild.Measure(availableSize);
                }
                return default;
            }
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
