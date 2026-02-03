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

using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.OpenStreetMapControl
{
    [ContentProperty("Source")]
    public class TransformableMapImage : Image
    {
        [Bindable(true)]
        public double WestLongitude
        { 
            get => (double)GetValue(WestLongitudeProperty); 
            set => SetValue(WestLongitudeProperty, value); 
        }
        public static readonly DependencyProperty WestLongitudeProperty =
            DependencyProperty.Register(nameof(WestLongitude), typeof(double), typeof(TransformableMapImage),
                new FrameworkPropertyMetadata(InvalidatePositionRect) { BindsTwoWayByDefault = true });


        [Bindable(true)]
        public double SouthLatitude
        { 
            get => (double)GetValue(SouthLatitudeProperty); 
            set => SetValue(SouthLatitudeProperty, value); 
        }
        public static readonly DependencyProperty SouthLatitudeProperty =
            DependencyProperty.Register(nameof(SouthLatitude), typeof(double), typeof(TransformableMapImage),
                new FrameworkPropertyMetadata(InvalidatePositionRect) { BindsTwoWayByDefault = true });


        [Bindable(true)]
        public double PixelPerMeter
        { 
            get => (double)GetValue(PixelPerMeterProperty); 
            set => SetValue(PixelPerMeterProperty, value); 
        }
        public static readonly DependencyProperty PixelPerMeterProperty =
            DependencyProperty.Register(nameof(PixelPerMeter), typeof(double), typeof(TransformableMapImage),
                new FrameworkPropertyMetadata(InvalidatePositionRect) { BindsTwoWayByDefault = true });
        
        private static void InvalidatePositionRect(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as TransformableMapImage)?.RequestUpdatePositionRectOnNextRender();


        [Bindable(true)]
        public double Rotation
        { 
            get => (double)GetValue(RotationProperty); 
            set => SetValue(RotationProperty, value); 
        }
        public static readonly DependencyProperty RotationProperty =
            DependencyProperty.Register(nameof(Rotation), typeof(double), typeof(TransformableMapImage),
                new FrameworkPropertyMetadata(OnRotationChanged) { BindsTwoWayByDefault = true });
        // 핸들러의 RotateTransform 조작 자체가 렌더링을 유발하므로 AffectsRender = true 필요하지 않음
        private static void OnRotationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        { if(d is TransformableMapImage instance) instance.rotateTransform.Angle = (double)e.NewValue; }


        [Bindable(true)]
        public ImageSource TinyImage
        { 
            get => (ImageSource)GetValue(TinyImageProperty); 
            set => SetValue(TinyImageProperty, value); 
        }
        public static readonly DependencyProperty TinyImageProperty =
            DependencyProperty.Register(nameof(TinyImage), typeof(ImageSource), typeof(TransformableMapImage));


        // Source 변경 시 null이 아닐 경우 렌더링 걸고, null일 경우 요청 해제
        private static void OverrideSourceProperty() => SourceProperty.OverrideMetadata(
            typeof(TransformableMapImage), new FrameworkPropertyMetadata(OnSourceChanged));
        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is not TransformableMapImage instance) return;
            if(e.NewValue != null && e.NewValue is not BitmapSource)
                throw new InvalidOperationException($"{nameof(TransformableMapImage)} only supports BitmapSource as Image.Source.");
            if(instance.Source != null) instance.RequestUpdatePositionRectOnNextRender();
            else instance.UnsubscribeRendering();
        }

        // 커서 설정
        public Cursor GrabCursor { get; set; }
        public Cursor UngrabCursor { get; set; }

        private Map rootMap;
        private MapLayer parentLayer;

        // 드래그 상태 변수
        private bool isOnDrag;
        private TransformMode mode;
        private enum TransformMode { Translation, RotationScaling }
        private Point dragAnchor, lastPosInRootMap;
        private double dragStartedPPM;

        // PositionRect 갱신 변수
        private bool isPositionRectValid, isSubscribingRendering;

        private readonly RotateTransform rotateTransform; // 렌더링에 직접 영향

        static TransformableMapImage()
        {
            OverrideSourceProperty();
            InitializePens();
        }

        public TransformableMapImage()
        {
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Bottom;

            // 드래그 상태
            isOnDrag = false;
            
            // RotateTransform
            rotateTransform = new RotateTransform();
            RenderTransform = rotateTransform;
            RenderTransformOrigin = new Point(0, 1);

            // PositionRect 갱신 변수
            isPositionRectValid = true;
            isSubscribingRendering = false;

            IsHitTestVisibleChanged += (sender, e) => InvalidateVisual();

            // Loaded 시 한 번만 PositionRect 갱신 및 렌더링을 요청
            Loaded += (sender, e) => RequestUpdatePositionRectOnNextRender();
            Unloaded += (sender, e) => UnsubscribeRendering();

            // Ungrab Cursor late load
            Loaded += loadUngrabCursor;
            void loadUngrabCursor(object sender, RoutedEventArgs e)
            {
                Loaded -= loadUngrabCursor;
                if(UngrabCursor != null) Cursor = UngrabCursor;
            }
        }

        /// <summary>
        /// 다음 렌더링 타이밍에 PositionRect 가 연산 및 커밋되도록 예약. 
        /// 이후 두 프레임의 렌더링을 유발 (다음 렌더링에서 Measure 유발 -> 렌더링 한 번 더). 
        /// 중복으로 호출되더라도 한 프레임에 한 번만 PositionRect의 커밋이 발생하도록 하기 위해, 
        /// 렌더링 이벤트에 Pending하는 로직으로서, 렌더링 이벤트 구독을 포함.
        /// </summary>
        private void RequestUpdatePositionRectOnNextRender()
        {
            //if(!isPositionRectValid) return; // 중복호출 guard
            isPositionRectValid = false; // PositionRect 무효화
            SubscribeRendering(); // 렌더링 이벤트 구독
            InvalidateVisual(); // 렌더링 트리거
        }


        // 렌더링 이벤트 구독
        private void SubscribeRendering()
        {
            if(isSubscribingRendering) return;
            isSubscribingRendering = true;
            CompositionTarget.Rendering += CalculateCommitPositionRect;
        }

        // 렌더링 이벤트 구독 해제
        private void UnsubscribeRendering()
        {
            if(!isSubscribingRendering) return;
            isSubscribingRendering = false;
            CompositionTarget.Rendering -= CalculateCommitPositionRect;
        }


        // 현재 Transform DP 및 드래그 관련 상태변수로 PositionRect 업데이트
        private void CalculateCommitPositionRect(object sender, EventArgs e)
        {
            if(isOnDrag) UpdateTransformPropertiesFromDragState(); //  드래그 고려

            // 렌더링 리소스 유효성 검사
            double ppm = PixelPerMeter;
            if(isPositionRectValid || Source == null || !IsValidPositiveDouble(ppm)) return;
            double imageWidth = ((BitmapSource)Source).PixelWidth;
            double imageHeight = ((BitmapSource)Source).PixelHeight;
            if(!IsValidPositiveDouble(imageWidth) || !IsValidPositiveDouble(imageHeight)) return;

            UnsubscribeRendering(); // 렌더링 구독해제
            isPositionRectValid = true; // PositionRect 유효화

            // Position Rect 계산
            double west = WestLongitude;
            double south = SouthLatitude;
            (double east, double north) = TranslateWGSPoint(new Point(west, south), imageWidth / ppm, imageHeight / ppm);
            SetValue(MapLayer.PositionRectangleProperty, new LocationRect(north, west, south, east));
            parentLayer?.InvalidateMeasure(); // PositionRect 업데이트 자체는 parent measure를 유발하지 않으므로 명시적 호출 필요
        }


        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // 거의 발생하지 않지만, 드래그 중인 상태에서 MouseUp 없이
            // 새로 드래그가 시작될 경우에 대한 처리
            if(isOnDrag) UpdateTransformPropertiesFromDragState();

            // 모드 할당
            Point clickPosition = e.GetPosition(this);
            mode = (clickPosition.X > ActualWidth - 20 && clickPosition.Y < 20) ? 
                TransformMode.RotationScaling : TransformMode.Translation;

            // dragAnchor 계산 및 lastPosInRootMap 초기화
            LocationRect dragStartedLR = MapLayer.GetPositionRectangle(this);
            rootMap.TryLocationToViewportPoint(new Location(dragStartedLR.South, dragStartedLR.West), out Point swOnView);
            lastPosInRootMap = e.GetPosition(rootMap);
            dragAnchor = lastPosInRootMap - (Vector)swOnView;

            // drag 시작 PPM 할당
            dragStartedPPM = PixelPerMeter;

            // 드래그 시작
            isOnDrag = true;
            CaptureMouse();
            if(GrabCursor != null) Cursor = GrabCursor;  // 커서 변경
            e.Handled = true;
        }


        protected override void OnMouseMove(MouseEventArgs e)
        {
            if(!(IsMouseCaptured && isOnDrag)) return;  // 드래그 아닐 경우 탈출
            lastPosInRootMap = e.GetPosition(rootMap);  // 지속적으로 최근 마우스 위치 update (최소 연산)
            RequestUpdatePositionRectOnNextRender();    // PositionRect 갱신 요청
            e.Handled = true;
        }


        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if(!IsMouseCaptured) return;
            ReleaseMouseCapture();
            if(UngrabCursor != null) Cursor = UngrabCursor;  // 커서 변경

            // 드래그 중이었을 경우 Transform DP 
            if(isOnDrag)
            {
                RequestUpdatePositionRectOnNextRender();
                isOnDrag = false;
                e.Handled = true;
            }
        }


        // 드래그 관련 상태변수로 Transform DP를 업데이트
        private void UpdateTransformPropertiesFromDragState()
        {
            switch(mode)
            {
                case TransformMode.RotationScaling:
                    // Rotation 처리
                    Point curVector = lastPosInRootMap - (Vector)TranslatePoint(new Point(), rootMap);
                    var rotation = 180 / Math.PI * Math.Atan2(curVector.Y, curVector.X);
                    Rotation = rotation;

                    // Scaling 처리
                    rootMap.TryLocationToViewportPoint(new Location(SouthLatitude, WestLongitude), out Point swOnView);
                    Point curPos = lastPosInRootMap - (Vector)swOnView;
                    double curPosLen = ((Vector)curPos).Length;
                    double dragAnchorLen = ((Vector)dragAnchor).Length;
                    if(!(IsValidPositiveDouble(curPosLen) && IsValidPositiveDouble(dragAnchorLen))) break;
                    PixelPerMeter = dragStartedPPM * (dragAnchorLen / curPosLen);
                    break;

                case TransformMode.Translation:
                    Point movedPos = lastPosInRootMap - (Vector)dragAnchor;
                    rootMap.TryViewportPointToLocation(movedPos, out Location movedLoc);
                    WestLongitude = movedLoc.Longitude;
                    SouthLatitude = movedLoc.Latitude;
                    break;
            }
        }

        private static Pen simplePen, horiDash, vertDash;

        protected override void OnRender(DrawingContext dc)
        {
            // TinyImage가 존재하면서, 렌더링 크기가 매우 작을 경우 이로 대체
            if(TinyImage != null && ActualHeight < 24 && ActualWidth < 24)
            {
                dc.PushTransform(new RotateTransform(-rotateTransform.Angle));
                dc.DrawImage(TinyImage, new Rect(-24, -24, 48, 48));
                dc.Pop();
            }
            else // 이외의 경우 이미지 정상 렌더링 및 이미지 외곽선, 변환 컨트롤 렌더링
            {
                base.OnRender(dc);
                if(!IsHitTestVisible) return;
                dc.DrawEllipse(Brushes.Black, simplePen, new Point(0, ActualHeight), 2, 2);
                dc.DrawLine(horiDash, new Point(0, 0), new Point(ActualWidth, 0));
                dc.DrawLine(vertDash, new Point(ActualWidth, 0), new Point(ActualWidth, ActualHeight));
                dc.DrawLine(horiDash, new Point(ActualWidth, ActualHeight), new Point(0, ActualHeight));
                dc.DrawLine(vertDash, new Point(0, ActualHeight), new Point(0, 0));
                dc.DrawImage(Application.Current.Resources["ShapeModImage"] as ImageSource, new Rect(ActualWidth - 10, -10, 20, 20));
            }
        }


        private static void InitializePens()
        {
            simplePen = new Pen(Brushes.Black, 1);
            simplePen.Freeze();

            var monoPalette = new BitmapPalette(new List<Color>() { Colors.Transparent, Colors.Black });
            var horiBrushBitmap = new WriteableBitmap(8, 8, 96, 96, PixelFormats.Indexed1, monoPalette);
            horiBrushBitmap.WritePixels(new Int32Rect(0, 0, 8, 8), new byte[] { 15, 15, 15, 15, 15, 15, 15, 15 }, 1, 0);
            horiBrushBitmap.Freeze();
            var horiBrush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 4, 4),
                ViewportUnits = BrushMappingMode.Absolute,
                Drawing = new ImageDrawing(horiBrushBitmap, new Rect(0, 0, 8, 8))
            };
            horiBrush.Freeze();
            horiDash = new Pen(horiBrush, 1);
            horiDash.Freeze();

            var vertBrushBitmap = new WriteableBitmap(8, 8, 96, 96, PixelFormats.Indexed1, monoPalette);
            vertBrushBitmap.WritePixels(new Int32Rect(0, 0, 8, 8), new byte[] { 255, 255, 255, 255, 0, 0, 0, 0 }, 1, 0);
            vertBrushBitmap.Freeze();
            var vertBrush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 4, 4),
                ViewportUnits = BrushMappingMode.Absolute,
                Drawing = new ImageDrawing(vertBrushBitmap, new Rect(0, 0, 8, 8))
            };
            vertBrush.Freeze();
            vertDash = new Pen(vertBrush, 1);
            vertDash.Freeze();
        }


        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            for(DependencyObject cur = this; cur != null; cur = VisualTreeHelper.GetParent(cur))
                if(cur is Map rt) { rootMap = rt; break; }

            for(DependencyObject cur = this; cur != null; cur = VisualTreeHelper.GetParent(cur))
                if(cur is MapLayer ml) { parentLayer = ml; break; }

            if(parentLayer != null) RequestUpdatePositionRectOnNextRender();
        }



        private const double EarthRadius = 6378137.0; // WGS-84 기준 지구 반경 (meters)
        private const double TO_RAD_COEF = Math.PI / 180.0;
        private const double TO_DEG_COEF = 180.0 / Math.PI;

        private static (double lon, double lat) TranslateWGSPoint(Point originLonLat, double xTranslationMeter, double yTranslationMeter)
        {
            double latRad = originLonLat.Y * TO_RAD_COEF;
            double lonRad = originLonLat.X * TO_RAD_COEF;

            double distance = Math.Sqrt(xTranslationMeter * xTranslationMeter + yTranslationMeter * yTranslationMeter);
            double bearing = Math.Atan2(xTranslationMeter, yTranslationMeter);

            double newLatRad = Math.Asin(Math.Sin(latRad) * Math.Cos(distance / EarthRadius) +
                                         Math.Cos(latRad) * Math.Sin(distance / EarthRadius) * Math.Cos(bearing));
            double newLonRad = lonRad + Math.Atan2(Math.Sin(bearing) * Math.Sin(distance / EarthRadius) * Math.Cos(latRad),
                                                   Math.Cos(distance / EarthRadius) - Math.Sin(latRad) * Math.Sin(newLatRad));

            return (newLonRad * TO_DEG_COEF, newLatRad * TO_DEG_COEF);
        }

        private static bool IsValidPositiveDouble(double value) => value > 1e-6 && value < 1e+100;
    }
}