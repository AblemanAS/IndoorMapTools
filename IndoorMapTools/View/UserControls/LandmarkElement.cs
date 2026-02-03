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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media.Effects;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace IndoorMapTools.View.UserControls
{
    public class LandmarkElement : UserControl
    {
        private const double WING_ROTATION_RAD = 0.4;
        private static readonly double COS_WING_ROTATION = Math.Cos(WING_ROTATION_RAD);
        private static readonly double SIN_WING_ROTATION = Math.Sin(WING_ROTATION_RAD);

        private static readonly Brush POLYGON_FILL_NORMAL = new SolidColorBrush(Colors.AliceBlue) { Opacity = 0.6 };
        private static readonly Brush POLYGON_FILL_HIGH = new SolidColorBrush(Colors.LightBlue) { Opacity = 0.6 };
        private static readonly DropShadowEffect ICON_EFFECT_NORMAL = null;
        private static readonly DropShadowEffect ICON_EFFECT_HIGH = new() { Color = Colors.Blue, ShadowDepth = 0, BlurRadius = 0 };
        private static readonly DropShadowEffect ARROW_EFFECT_NORMAL = null;
        private static readonly DropShadowEffect ARROW_EFFECT_HIGH = new() { Color = Colors.AliceBlue, ShadowDepth = 0, BlurRadius = 10 };

        [Bindable(true)]
        public Point Location
        {
            get => (Point)GetValue(LocationProperty);
            set => SetValue(LocationProperty, value);
        }
        public static readonly DependencyProperty LocationProperty = DependencyProperty.Register(nameof(Location),
            typeof(Point), typeof(LandmarkElement), new FrameworkPropertyMetadata(OnLocationChanged));

        [Bindable(true)]
        public double CoordinateScale
        {
            get => (double)GetValue(CoordinateScaleProperty);
            set => SetValue(CoordinateScaleProperty, value);
        }
        public static readonly DependencyProperty CoordinateScaleProperty =
            DependencyProperty.Register(nameof(CoordinateScale), typeof(double), typeof(LandmarkElement),
                new FrameworkPropertyMetadata(1.0, OnCoordinateScaleChanged));

        private static void OnCoordinateScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is LandmarkElement instance && e.NewValue is double scale)) return;
            instance.OnLocationChanged(instance.Location, scale);
            instance.UpdatePolygonGeometry(instance.PolygonPoints, scale);
        }

        private static void OnLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is LandmarkElement instance && e.NewValue is Point loc)) return;
            instance.OnLocationChanged(loc, instance.CoordinateScale);
        }

        private void OnLocationChanged(Point loc, double scale)
        {
            Canvas.SetLeft(icon, loc.X * scale);
            Canvas.SetTop(icon, loc.Y * scale);
            Canvas.SetLeft(arrow, loc.X * scale);
            Canvas.SetTop(arrow, loc.Y * scale);
        }

        [Bindable(true)]
        public bool IsHighlighted
        {
            get => (bool)GetValue(IsHighlightedProperty);
            set => SetValue(IsHighlightedProperty, value);
        }
        public static readonly DependencyProperty IsHighlightedProperty = DependencyProperty.Register(nameof(IsHighlighted),
            typeof(bool), typeof(LandmarkElement), new FrameworkPropertyMetadata(OnIsHighlightedChanged));

        private static void OnIsHighlightedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is LandmarkElement instance && e.NewValue is bool value)) return;
            instance.UpdateHighlighted(value);
        }
        
        [Bindable(true)]
        public ImageSource IconImageSource
        {
            get => (ImageSource)GetValue(IconImageSourceProperty);
            set => SetValue(IconImageSourceProperty, value);
        }
        public static readonly DependencyProperty IconImageSourceProperty = DependencyProperty.Register(nameof(IconImageSource), 
            typeof(ImageSource), typeof(LandmarkElement), new FrameworkPropertyMetadata(OnIconImageSourceChanged));

        private static void OnIconImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is LandmarkElement instance && e.NewValue is ImageSource source)) return;
            instance.icon.Source = source;
        }

        [Bindable(true)]
        public double IconWidth
        {
            get => (double)GetValue(IconWidthProperty);
            set => SetValue(IconWidthProperty, value);
        }
        public static readonly DependencyProperty IconWidthProperty = DependencyProperty.Register(nameof(IconWidth),
            typeof(double), typeof(LandmarkElement), new FrameworkPropertyMetadata(64.0, OnIconWidthChanged));

        private static void OnIconWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is LandmarkElement instance && e.NewValue is double width)) return;
            instance.icon.Width = width;
        }

        [Bindable(true)]
        public double IconHeight
        {
            get => (double)GetValue(IconHeightProperty);
            set => SetValue(IconHeightProperty, value);
        }
        public static readonly DependencyProperty IconHeightProperty = DependencyProperty.Register(nameof(IconHeight),
            typeof(double), typeof(LandmarkElement), new FrameworkPropertyMetadata(64.0, OnIconHeightChanged));

        private static void OnIconHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is LandmarkElement instance && e.NewValue is double height)) return;
            instance.icon.Height = height;
        }

        [Bindable(true)]
        public bool IsIconVisible
        {
            get => (bool)GetValue(IsIconVisibleProperty);
            set => SetValue(IsIconVisibleProperty, value);
        }
        public static readonly DependencyProperty IsIconVisibleProperty = DependencyProperty.Register(nameof(IsIconVisible), 
            typeof(bool), typeof(LandmarkElement), new FrameworkPropertyMetadata(true, OnIsIconVisibleChanged));

        private static void OnIsIconVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is LandmarkElement instance && e.NewValue is bool isVisible)) return;
            instance.icon.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        [Bindable(true)]
        public double ArrowDirection
        {
            get => (double)GetValue(ArrowDirectionProperty);
            set => SetValue(ArrowDirectionProperty, value);
        }
        public static readonly DependencyProperty ArrowDirectionProperty =
            DependencyProperty.Register(nameof(ArrowDirection), typeof(double), typeof(LandmarkElement),
                new FrameworkPropertyMetadata(0.0, OnArrowDirectionChanged));

        private static void OnArrowDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is LandmarkElement instance && e.NewValue is double dir)) return;
            instance.arrow.RenderTransform = new RotateTransform(dir);
        }

        [Bindable(true)]
        public int ArrowLength
        {
            get => (int)GetValue(ArrowLengthProperty);
            set => SetValue(ArrowLengthProperty, value);
        }
        public static readonly DependencyProperty ArrowLengthProperty =
            DependencyProperty.Register(nameof(ArrowLength), typeof(int), typeof(LandmarkElement),
                new FrameworkPropertyMetadata(60, OnArrowVisualChanged));

        [Bindable(true)]
        public int ArrowHeadSize
        {
            get => (int)GetValue(ArrowHeadSizeProperty);
            set => SetValue(ArrowHeadSizeProperty, value);
        }
        public static readonly DependencyProperty ArrowHeadSizeProperty =
            DependencyProperty.Register(nameof(ArrowHeadSize), typeof(int), typeof(LandmarkElement),
                new FrameworkPropertyMetadata(6, OnArrowVisualChanged));

        [Bindable(true)]
        public int ArrowHeads // -1:진입, 0:진입/탈출, 1:탈출
        {
            get => (int)GetValue(ArrowHeadsProperty);
            set => SetValue(ArrowHeadsProperty, value);
        }
        public static readonly DependencyProperty ArrowHeadsProperty =
            DependencyProperty.Register(nameof(ArrowHeads), typeof(int), typeof(LandmarkElement),
                new FrameworkPropertyMetadata(0, OnArrowVisualChanged));

        private static void OnArrowVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is not LandmarkElement instance) return;
            instance.arrow.UpdateGeometry(instance.ArrowLength, instance.ArrowHeadSize,
                instance.ArrowHeads != 1, instance.ArrowHeads != -1);
        }

        [Bindable(true)]
        public bool IsArrowVisible
        {
            get => (bool)GetValue(IsArrowVisibleProperty);
            set => SetValue(IsArrowVisibleProperty, value);
        }
        public static readonly DependencyProperty IsArrowVisibleProperty = DependencyProperty.Register(nameof(IsArrowVisible), 
            typeof(bool), typeof(LandmarkElement), new FrameworkPropertyMetadata(true, OnIsArrowVisibleChanged));

        private static void OnIsArrowVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is LandmarkElement instance && e.NewValue is bool isVisible)) return;
            instance.arrow.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        [Bindable(true)]
        public IEnumerable<Point> PolygonPoints
        {
            get => (IEnumerable<Point>)GetValue(PolygonPointsProperty);
            set => SetValue(PolygonPointsProperty, value);
        }
        public static readonly DependencyProperty PolygonPointsProperty =
            DependencyProperty.Register(nameof(PolygonPoints), typeof(IEnumerable<Point>), typeof(LandmarkElement),
                new FrameworkPropertyMetadata(OnPolygonPointsChanged));

        private static void OnPolygonPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is not LandmarkElement instance) return;

            instance.UpdatePolygonGeometry(e.NewValue as IEnumerable<Point>, instance.CoordinateScale);
            if(e.OldValue is INotifyCollectionChanged inccOld)
                inccOld.CollectionChanged -= instance.OnPolygonPointsCollectionChanged;
            if(e.NewValue is INotifyCollectionChanged inccNew) 
                inccNew.CollectionChanged += instance.OnPolygonPointsCollectionChanged;
        }

        private void OnPolygonPointsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            => UpdatePolygonGeometry(PolygonPoints, CoordinateScale);

        [Bindable(true)]
        public bool IsPolygonVisible
        {
            get => (bool)GetValue(IsPolygonVisibleProperty);
            set => SetValue(IsPolygonVisibleProperty, value);
        }
        public static readonly DependencyProperty IsPolygonVisibleProperty = DependencyProperty.Register(nameof(IsPolygonVisible),
            typeof(bool), typeof(LandmarkElement), new FrameworkPropertyMetadata(true, OnIsPolygonVisibleChanged));

        private static void OnIsPolygonVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is LandmarkElement instance && e.NewValue is bool isVisible)) return;
            instance.polygon.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private readonly Sprite icon;
        private readonly Arrow arrow;
        private readonly Polygon polygon;

        public LandmarkElement()
        {
            icon = new Sprite { Width = 64, Height = 64 };
            arrow = new Arrow { Stroke = Brushes.Black, StrokeThickness = 3 }; 
            polygon = new Polygon { Stroke = Brushes.Black, StrokeThickness = 1.0, StrokeLineJoin = PenLineJoin.Bevel };

            var mainCanvas = new Canvas();
            mainCanvas.Children.Add(polygon);
            mainCanvas.Children.Add(icon);
            mainCanvas.Children.Add(arrow);
            Content = mainCanvas;

            arrow.UpdateGeometry(ArrowLength, ArrowHeadSize, ArrowHeads != 1, ArrowHeads != -1);
            UpdateHighlighted(false);
        }

        public void UpdateHighlighted(bool value)
        {
            polygon.Fill = value ? POLYGON_FILL_HIGH : POLYGON_FILL_NORMAL;
            icon.Effect = value ? ICON_EFFECT_HIGH : ICON_EFFECT_NORMAL;
            arrow.Effect = value ? ARROW_EFFECT_HIGH : ARROW_EFFECT_NORMAL;
        }

        public void UpdatePolygonGeometry(IEnumerable<Point> points, double scale)
        {
            if(points == null) return;
            var pointsList = new List<Point>();
            foreach(Point curPoint in points)
                pointsList.Add(new Point(curPoint.X * scale, curPoint.Y * scale));
            polygon.Points = new PointCollection(pointsList);
        }

        private class Sprite : Image
        {
            protected override void OnRender(DrawingContext dc)
            { if(Source != null) dc.DrawImage(Source, new Rect(-Width / 2, -Height / 2, Width, Height)); }
        }

        private class Arrow : Shape
        {
            private Geometry definingGeometry;
            protected override Geometry DefiningGeometry => definingGeometry;

            private (int arrowLen, int headSize, bool isStartHeaded, bool isEndHeaded) cacheParam;
            private (int arrowLen, int headSize, bool isStartHeaded, bool isEndHeaded) calledParam;

            public void UpdateGeometry(int arrowLen, int headSize, bool isStartHeaded, bool isEndHeaded)
            {
                calledParam = (arrowLen, headSize, isStartHeaded, isEndHeaded);
                InvalidateMeasure();
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                if(calledParam.arrowLen != cacheParam.arrowLen || calledParam.headSize != cacheParam.headSize ||
                   calledParam.isStartHeaded != cacheParam.isStartHeaded ||
                   calledParam.isEndHeaded != cacheParam.isEndHeaded)
                    BuildGeometry(calledParam.arrowLen, calledParam.headSize, calledParam.isStartHeaded, calledParam.isEndHeaded);

                return base.MeasureOverride(availableSize);
            }

            private void BuildGeometry(int arrowLen, int headSize, bool isStartHeaded, bool isEndHeaded)
            {
                var arrowGeometry = new PathGeometry();
                Point target = new Point(arrowLen, 0);

                // 몸통 Figure
                var bodyFigure = new PathFigure();
                bodyFigure.Segments.Add(new LineSegment(target, true) { IsSmoothJoin = true });
                arrowGeometry.Figures.Add(bodyFigure);

                double headSizeCos = headSize * COS_WING_ROTATION;
                double headSizeSin = headSize * SIN_WING_ROTATION;

                // 원점 헤드
                if(isStartHeaded)
                {
                    var startHeadFigure = new PathFigure() { IsClosed = true };
                    var startHeadSegment = new PolyLineSegment();
                    startHeadSegment.Points.Add(new Point(headSizeCos, headSizeSin));
                    startHeadSegment.Points.Add(new Point(headSizeCos, -headSizeSin));
                    startHeadFigure.Segments.Add(startHeadSegment);
                    arrowGeometry.Figures.Add(startHeadFigure);
                }

                // 끝점 헤드
                if(isEndHeaded)
                {
                    var endHeadFigure = new PathFigure() { IsClosed = true, StartPoint = target };
                    var endHeadSegment = new PolyLineSegment();
                    endHeadSegment.Points.Add(new Point(arrowLen - headSizeCos, headSizeSin));
                    endHeadSegment.Points.Add(new Point(arrowLen - headSizeCos, -headSizeSin));
                    endHeadFigure.Segments.Add(endHeadSegment);
                    arrowGeometry.Figures.Add(endHeadFigure);
                }

                definingGeometry = arrowGeometry;
                cacheParam.arrowLen = arrowLen;
                cacheParam.headSize = headSize;
                cacheParam.isStartHeaded = isStartHeaded;
                cacheParam.isEndHeaded = isEndHeaded;
            }

        }
    }
}
