using IndoorMapTools.Core;
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
using System.Windows.Threading;

namespace IndoorMapTools.OpenStreetMapControl
{
    [ContentProperty("Source")]
    public class TransformableMapImage : Image
    {
        [Bindable(true)]
        public double WestLongitude
        { get => (double)GetValue(WestLongitudeProperty); set => SetValue(WestLongitudeProperty, value); }
        public static readonly DependencyProperty WestLongitudeProperty =
            DependencyProperty.Register(nameof(WestLongitude), typeof(double), typeof(TransformableMapImage),
                new FrameworkPropertyMetadata(OnWestLongitudeChanged) { BindsTwoWayByDefault = true });

        private static void OnWestLongitudeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is TransformableMapImage instance)) return;
            instance.west = (double)e.NewValue;
            instance.UpdatePositionRect();
        }

        [Bindable(true)]
        public double SouthLatitude
        { get => (double)GetValue(SouthLatitudeProperty); set => SetValue(SouthLatitudeProperty, value); }
        public static readonly DependencyProperty SouthLatitudeProperty =
            DependencyProperty.Register(nameof(SouthLatitude), typeof(double), typeof(TransformableMapImage),
                new FrameworkPropertyMetadata(OnSouthLatitudeChanged) { BindsTwoWayByDefault = true });

        private static void OnSouthLatitudeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is TransformableMapImage instance)) return;
            instance.south = (double)e.NewValue;
            instance.UpdatePositionRect();
        }

        [Bindable(true)]
        public double PixelPerMeter
        { get => (double)GetValue(PixelPerMeterProperty); set => SetValue(PixelPerMeterProperty, value); }
        public static readonly DependencyProperty PixelPerMeterProperty =
            DependencyProperty.Register(nameof(PixelPerMeter), typeof(double), typeof(TransformableMapImage),
                new FrameworkPropertyMetadata(OnPixelPerMeterChanged) { BindsTwoWayByDefault = true });

        private static void OnPixelPerMeterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is TransformableMapImage instance)) return;
            instance.ppm = (double)e.NewValue;
            instance.UpdatePositionRect();
        }

        [Bindable(true)]
        public double Rotation
        { get => (double)GetValue(RotationProperty); set => SetValue(RotationProperty, value); }
        public static readonly DependencyProperty RotationProperty =
            DependencyProperty.Register(nameof(Rotation), typeof(double), typeof(TransformableMapImage),
                new FrameworkPropertyMetadata(OnRotationChanged) { BindsTwoWayByDefault = true });

        private static void OnRotationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is TransformableMapImage instance)) return;
            instance.rotation = (double)e.NewValue;
            instance.rotateTransform.Angle = instance.rotation;
        }

        [Bindable(true)]
        public ImageSource TinyImage
        { get => (ImageSource)GetValue(TinyImageProperty); set => SetValue(TinyImageProperty, value); }
        public static readonly DependencyProperty TinyImageProperty =
            DependencyProperty.Register(nameof(TinyImage), typeof(ImageSource), typeof(TransformableMapImage));

        private Map rootMap;
        private MapLayer parentLayer;
        private Point dragAnchor;
        private enum TransformMode { Translation, RotationScaling }
        private TransformMode mode;

        private readonly RotateTransform rotateTransform;
        private double west, south, ppm, rotation;

        public TransformableMapImage()
        {
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Bottom;

            // RotateTransform
            rotateTransform = new RotateTransform();
            RenderTransform = rotateTransform;
            RenderTransformOrigin = new Point(0, 1);

            if(simplePen == null) InitializePens();

            IsHitTestVisibleChanged += (sender, e) => InvalidateVisual();
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

        private static Pen simplePen, horiDash, vertDash;

        protected override void OnRender(DrawingContext dc)
        {
            if(TinyImage != null && ActualHeight < 24 && ActualWidth < 24)
            {
                dc.PushTransform(new RotateTransform(-rotateTransform.Angle));
                dc.DrawImage(TinyImage, new Rect(-24, -24, 48, 48));
                dc.Pop();
            }
            else
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


        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            Point clickPosition = e.GetPosition(this);
            LocationRect clickedLocationRect = MapLayer.GetPositionRectangle(this);
            rootMap.TryLocationToViewportPoint(new Location(clickedLocationRect.South, clickedLocationRect.West), out Point swOnView);
            dragAnchor = e.GetPosition(rootMap) - (Vector)swOnView;

            if(clickPosition.X > ActualWidth - 20 && clickPosition.Y < 20)
                mode = TransformMode.RotationScaling;
            else mode = TransformMode.Translation;

            CaptureMouse();
            e.Handled = true;
        }


        protected override void OnMouseMove(MouseEventArgs e)
        {
            if(IsMouseCaptured)
            {
                switch(mode)
                {
                    case TransformMode.RotationScaling:
                        Point curVector = e.GetPosition(rootMap) - (Vector)TranslatePoint(new Point(), rootMap);
                        rotation = 180 / Math.PI * Math.Atan2(curVector.Y, curVector.X);
                        rotateTransform.Angle = rotation;

                        rootMap.TryLocationToViewportPoint(new Location(SouthLatitude, WestLongitude), out Point swOnView);
                        Point curAnchor = e.GetPosition(rootMap) - (Vector)swOnView;
                        ppm = PixelPerMeter * (((Vector)dragAnchor).Length / ((Vector)curAnchor).Length);
                        UpdatePositionRect();
                        break;

                    case TransformMode.Translation:
                        Point movedPos = e.GetPosition(rootMap) - (Vector)dragAnchor;
                        rootMap.TryViewportPointToLocation(movedPos, out Location movedLoc);
                        west = movedLoc.Longitude;
                        south = movedLoc.Latitude;
                        UpdatePositionRect();
                        break;
                }

                e.Handled = true;
            }
        }

        private void UpdatePositionRect()
        {
            if(ppm < 0.0 || Source == null) return;
            Point ne = GeoLocationModule.TranslateWGSPoint(new Point(west, south), Source.Width / ppm, Source.Height / ppm);
            SetValue(MapLayer.PositionRectangleProperty, new LocationRect(ne.Y, west, south, ne.X));
            parentLayer.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => parentLayer.InvalidateMeasure()));
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if(IsMouseCaptured)
            {
                switch(mode)
                {
                    case TransformMode.RotationScaling:
                        SetValue(RotationProperty, rotation);
                        SetValue(PixelPerMeterProperty, ppm);
                        break;

                    case TransformMode.Translation:
                        SetValue(WestLongitudeProperty, west);
                        SetValue(SouthLatitudeProperty, south);
                        break;
                }
            }

            ReleaseMouseCapture();

            e.Handled = true;
        }


        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            for(DependencyObject cur = this; cur != null; cur = VisualTreeHelper.GetParent(cur))
                if(cur is Map rt) { rootMap = rt; break; }

            for(DependencyObject cur = this; cur != null; cur = VisualTreeHelper.GetParent(cur))
                if(cur is MapLayer ml) { parentLayer = ml; break; }
        }
    }

}
