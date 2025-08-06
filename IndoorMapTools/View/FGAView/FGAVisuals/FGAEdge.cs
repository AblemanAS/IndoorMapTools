using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace IndoorMapTools.View.FGAView.FGAVisuals
{
    public class FGAEdge : FGAShape
    {
        [Bindable(true)]
        public int Floor1
        {
            get => (int)GetValue(Floor1Property);
            set => SetValue(Floor1Property, value);
        }
        public static readonly DependencyProperty Floor1Property =
            DependencyProperty.Register(nameof(Floor1), typeof(int), typeof(FGAEdge),
                new FrameworkPropertyMetadata(-1) { AffectsArrange = true, AffectsMeasure = true, AffectsRender = true });

        [Bindable(true)]
        public int Group1
        {
            get => (int)GetValue(Group1Property);
            set => SetValue(Group1Property, value);
        }
        public static readonly DependencyProperty Group1Property =
            DependencyProperty.Register(nameof(Group1), typeof(int), typeof(FGAEdge),
                new FrameworkPropertyMetadata(-1) { AffectsArrange = true, AffectsMeasure = true, AffectsRender = true });

        [Bindable(true)]
        public int Area1
        {
            get => (int)GetValue(Area1Property);
            set => SetValue(Area1Property, value);
        }
        public static readonly DependencyProperty Area1Property =
            DependencyProperty.Register(nameof(Area1), typeof(int), typeof(FGAEdge),
                new FrameworkPropertyMetadata(-1) { AffectsArrange = true, AffectsMeasure = true, AffectsRender = true });

        [Bindable(true)]
        public int Floor2
        {
            get => (int)GetValue(Floor2Property);
            set => SetValue(Floor2Property, value);
        }
        public static readonly DependencyProperty Floor2Property =
            DependencyProperty.Register(nameof(Floor2), typeof(int), typeof(FGAEdge),
                new FrameworkPropertyMetadata(-1) { AffectsArrange = true, AffectsMeasure = true, AffectsRender = true });

        [Bindable(true)]
        public int Group2
        {
            get => (int)GetValue(Group2Property);
            set => SetValue(Group2Property, value);
        }
        public static readonly DependencyProperty Group2Property =
            DependencyProperty.Register(nameof(Group2), typeof(int), typeof(FGAEdge),
                new FrameworkPropertyMetadata(-1) { AffectsArrange = true, AffectsMeasure = true, AffectsRender = true });

        [Bindable(true)]
        public int Area2
        {
            get => (int)GetValue(Area2Property);
            set => SetValue(Area2Property, value);
        }
        public static readonly DependencyProperty Area2Property =
            DependencyProperty.Register(nameof(Area2), typeof(int), typeof(FGAEdge),
                new FrameworkPropertyMetadata(-1) { AffectsArrange = true, AffectsMeasure = true, AffectsRender = true });

        [Bindable(true)]
        public double Deduct1
        {
            get => (double)GetValue(Deduct1Property);
            set => SetValue(Deduct1Property, value);
        }
        public static readonly DependencyProperty Deduct1Property =
            DependencyProperty.Register(nameof(Deduct1), typeof(double), typeof(FGAEdge),
                new FrameworkPropertyMetadata(0.0) { AffectsArrange = true, AffectsMeasure = true, AffectsRender = true });

        [Bindable(true)]
        public double Deduct2
        {
            get => (double)GetValue(Deduct2Property);
            set => SetValue(Deduct2Property, value);
        }
        public static readonly DependencyProperty Deduct2Property =
            DependencyProperty.Register(nameof(Deduct2), typeof(double), typeof(FGAEdge),
                new FrameworkPropertyMetadata(0.0) { AffectsArrange = true, AffectsMeasure = true, AffectsRender = true });

        public enum EdgeHeader { None, Arrow, Circle }

        [Bindable(true)]
        public EdgeHeader HeadType
        {
            get => (EdgeHeader)GetValue(HeadTypeProperty);
            set => SetValue(HeadTypeProperty, value);
        }
        public static readonly DependencyProperty HeadTypeProperty =
            DependencyProperty.Register(nameof(HeadType), typeof(EdgeHeader), typeof(FGAEdge),
                new FrameworkPropertyMetadata(EdgeHeader.None) { AffectsArrange = true, AffectsMeasure = true, AffectsRender = true });

        [Bindable(true)]
        public EdgeHeader TailType
        {
            get => (EdgeHeader)GetValue(TailTypeProperty);
            set => SetValue(TailTypeProperty, value);
        }
        public static readonly DependencyProperty TailTypeProperty =
            DependencyProperty.Register(nameof(TailType), typeof(EdgeHeader), typeof(FGAEdge),
                new FrameworkPropertyMetadata(EdgeHeader.None) { AffectsArrange = true, AffectsMeasure = true, AffectsRender = true });

        /// <summary> 헤드 크기 (화살표 윙 직선 길이, 원 지름) </summary>
        [Bindable(true)]
        public int HeadSize
        {
            get => (int)GetValue(HeadSizeProperty);
            set => SetValue(HeadSizeProperty, value);
        }
        public static readonly DependencyProperty HeadSizeProperty =
            DependencyProperty.Register(nameof(HeadSize), typeof(int), typeof(FGAEdge),
                new FrameworkPropertyMetadata(6) { AffectsArrange = true, AffectsMeasure = true, AffectsRender = true });

        protected override Geometry DefiningGeometry => definingGeometry;
        private Geometry definingGeometry;

        public FGAEdge()
        {
            SetBinding(FillProperty, new Binding(nameof(Stroke)) { Source = this });
            SetCurrentValue(StrokeProperty, Brushes.Black);
            SetCurrentValue(StrokeThicknessProperty, 2.0);
        }

        public override void CacheDefiningGeometry(Func<int, int, int, Rect> mappingFunction)
        {
            var startRect = mappingFunction(Floor1, Group1, Area1);
            var endRect = mappingFunction(Floor2, Group2, Area2);
            var startPointRaw = startRect.TopLeft + (Vector)startRect.Size / 2;
            var endPointRaw = endRect.TopLeft + (Vector)endRect.Size / 2;

            var direction = endPointRaw - startPointRaw;
            direction.Normalize();

            var startPoint = startPointRaw + direction * Deduct1;
            var endPoint = endPointRaw - direction * Deduct2;

            var edgeGeometry = new LineGeometry(startPoint, endPoint);
            var geoGroup = new GeometryGroup { Children = { edgeGeometry } };

            switch(HeadType)
            {
                case EdgeHeader.Arrow:
                    // 직선의 역방향 단위 벡터
                    var baseDir = -direction * HeadSize;

                    // 수직 방향 벡터
                    var perp = new Vector(direction.Y, -direction.X) * (HeadSize / 2.0);

                    var wing1 = endPoint + baseDir + perp;
                    var wing2 = endPoint + baseDir - perp;

                    var arrowHead = new PathFigure
                    {
                        StartPoint = endPoint,
                        IsClosed = true,
                        IsFilled = true,
                        Segments = new PathSegmentCollection
                    {
                        new LineSegment(wing1, true),
                        new LineSegment(wing2, true)
                    }
                    };

                    var arrowGeometry = new PathGeometry(new[] { arrowHead });
                    geoGroup.Children.Add(arrowGeometry);
                    break;

                case EdgeHeader.Circle:
                    var circleGeometry = new EllipseGeometry(endPoint, HeadSize / 2.0, HeadSize / 2.0);
                    geoGroup.Children.Add(circleGeometry);
                    break;
            }

            switch(TailType)
            {
                case EdgeHeader.Arrow:
                    // 직선의 정방향 단위 벡터
                    var baseDir = direction * HeadSize;

                    // 수직 방향 벡터
                    var perp = new Vector(direction.Y, -direction.X) * (HeadSize / 2.0);

                    var wing1 = startPoint + baseDir + perp;
                    var wing2 = startPoint + baseDir - perp;

                    var arrowHead = new PathFigure
                    {
                        StartPoint = startPoint,
                        IsClosed = true,
                        IsFilled = true,
                        Segments = new PathSegmentCollection
                    {
                        new LineSegment(wing1, true),
                        new LineSegment(wing2, true)
                    }
                    };

                    var arrowGeometry = new PathGeometry(new[] { arrowHead });
                    geoGroup.Children.Add(arrowGeometry);
                    break;

                case EdgeHeader.Circle:
                    var circleGeometry = new EllipseGeometry(startPoint, HeadSize / 2.0, HeadSize / 2.0);
                    geoGroup.Children.Add(circleGeometry);
                    break;
            }

            definingGeometry = geoGroup;
        }
    }
}
