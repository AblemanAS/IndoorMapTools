﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IndoorMapTools.View.FGAView.FGAVisuals
{
    public class FGAArea : UserControl
    {
        private static readonly PathGeometry leftGeometry, rightGeometry;

        static FGAArea()
        {
            leftGeometry = new PathGeometry
            {
                Figures = new PathFigureCollection
                {
                    new PathFigure
                    {
                        StartPoint = new Point(4, 1),
                        Segments = new PathSegmentCollection
                        {
                            new LineSegment(new Point(8, 1), true),
                            new LineSegment(new Point(8, 7), true),
                            new LineSegment(new Point(4, 7), true),
                            new ArcSegment(new Point(4, 1), new Size(3, 3), 0, false, SweepDirection.Clockwise, true)
                        }
                    }
                }
            };
            leftGeometry.Freeze();

            rightGeometry = new PathGeometry
            {
                Figures = new PathFigureCollection
                {
                    new PathFigure
                    {
                        StartPoint = new Point(4, 1),
                        Segments = new PathSegmentCollection
                        {
                            new LineSegment(new Point(0, 1), true),
                            new LineSegment(new Point(0, 7), true),
                            new LineSegment(new Point(4, 7), true),
                            new ArcSegment(new Point(4, 1), new Size(3, 3), 0, false, SweepDirection.Counterclockwise, true)
                        }
                    }
                }
            };
            rightGeometry.Freeze();
        }

        public int GridSize { get; set; } = 64;

        [Bindable(true)]
        public int FloorId
        {
            get => (int)GetValue(FloorIdProperty);
            set => SetValue(FloorIdProperty, value);
        }
        public static readonly DependencyProperty FloorIdProperty =
            DependencyProperty.Register(nameof(FloorId), typeof(int), typeof(FGAArea));

        [Bindable(true)]
        public ICollection<int> GroupIds
        {
            get => (ICollection<int>)GetValue(GroupIdsProperty);
            set => SetValue(GroupIdsProperty, value);
        }
        public static readonly DependencyProperty GroupIdsProperty =
            DependencyProperty.Register(nameof(GroupIds), typeof(ICollection<int>), typeof(FGAArea),
                new FrameworkPropertyMetadata(OnGroupIdsChanged));

        private static void OnGroupIdsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as FGAArea)?.UpdateSegments();

        [Bindable(true)]
        public int AreaId
        {
            get => (int)GetValue(AreaIdProperty);
            set => SetValue(AreaIdProperty, value);
        }
        public static readonly DependencyProperty AreaIdProperty =
            DependencyProperty.Register(nameof(AreaId), typeof(int), typeof(FGAArea));

        private readonly FGAPanel innerPanel = new FGAPanel();

        public FGAArea() => Content = innerPanel;

        protected override Size MeasureOverride(Size constraint)
        {
            UpdateSegments();
            return base.MeasureOverride(constraint);
        }

        private void UpdateSegments()
        {
            innerPanel.Children.Clear();
            if(GroupIds == null || GroupIds.Count == 0) return;

            // Shape 렌더링 변수
            int innerMargin = GridSize / 16;    

            // 단일 Segment Area일 경우 원 하나만 추가
            if(GroupIds.Count == 1)
            {
                LocateSegment(new Ellipse { Fill = Brushes.AliceBlue, StrokeThickness = 0, Stretch = Stretch.Fill,
                    Margin = new Thickness(innerMargin) }, FloorId, GroupIds.First(), AreaId);
            }
            else // 단일 Segment Area가 아닐 경우, Left Segment 모양 변경 및 Right Segment 추가
            {
                // Left, Right index 구하기
                int minGroup = GroupIds.Min();
                int maxGroup = GroupIds.Max();

                // Left Segment 추가
                LocateSegment(new Path { Fill = Brushes.AliceBlue, StrokeThickness = 0, Stretch = Stretch.Fill, Data = leftGeometry,
                    Margin = new Thickness(innerMargin, innerMargin, 0, innerMargin) }, FloorId, minGroup, AreaId);

                // Right Segment 추가
                LocateSegment(new Path { Fill = Brushes.AliceBlue, StrokeThickness = 0, Stretch = Stretch.Fill, Data = rightGeometry,
                    Margin = new Thickness(0, innerMargin, innerMargin, innerMargin) }, FloorId, maxGroup, AreaId);

                // 중간 Segment 추가
                for(int i = minGroup + 1; i < maxGroup; i++)
                    LocateSegment(new Rectangle { Fill = Brushes.AliceBlue, StrokeThickness = 0, Stretch = Stretch.Fill,
                        Margin = new Thickness(0, innerMargin, 0, innerMargin) }, FloorId, i, AreaId);
            }
        }

        private void LocateSegment(FrameworkElement fe, int floor, int group, int area)
        {
            fe.SetValue(FGAPanel.FloorProperty, floor);
            fe.SetValue(FGAPanel.GroupProperty, group);
            fe.SetValue(FGAPanel.AreaProperty, area);
            innerPanel.Children.Add(fe);
        }
    }

}
