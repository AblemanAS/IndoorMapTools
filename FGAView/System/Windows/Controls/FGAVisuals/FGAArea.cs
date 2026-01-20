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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FGAView.System.Windows.Controls.FGAVisuals
{
    public class FGAArea : UserControl
    {
        private static readonly PathGeometry leftGeometry, rightGeometry;

        static FGAArea()
        {
            ForegroundProperty.OverrideMetadata(typeof(FGAArea), new FrameworkPropertyMetadata(OnForegroundChanged));

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

        private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is FGAArea instance)) return;
            foreach(var child in instance.innerPanel.Children)
                (child as Shape).Fill = instance.Foreground;
        }

        public int GridSize { get; set; } = 64;

        [Bindable(true)]
        public int FloorId
        {
            get => (int)GetValue(FloorIdProperty);
            set => SetValue(FloorIdProperty, value);
        }
        public static readonly DependencyProperty FloorIdProperty = DependencyProperty.Register(nameof(FloorId), 
            typeof(int), typeof(FGAArea), new FrameworkPropertyMetadata(OnFGALayoutDataChanged));

        [Bindable(true)]
        public ICollection<int> GroupIds
        {
            get => (ICollection<int>)GetValue(GroupIdsProperty);
            set => SetValue(GroupIdsProperty, value);
        }
        public static readonly DependencyProperty GroupIdsProperty = DependencyProperty.Register(nameof(GroupIds), 
            typeof(ICollection<int>), typeof(FGAArea), new FrameworkPropertyMetadata(OnFGALayoutDataChanged));

        [Bindable(true)]
        public int AreaId
        {
            get => (int)GetValue(AreaIdProperty);
            set => SetValue(AreaIdProperty, value);
        }
        public static readonly DependencyProperty AreaIdProperty = DependencyProperty.Register(nameof(AreaId), 
            typeof(int), typeof(FGAArea), new FrameworkPropertyMetadata(OnFGALayoutDataChanged));


        private static void OnFGALayoutDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is FGAArea instance)) return;
            instance.isSegmentsValid = false;
            instance.InvalidateMeasure();
        }

        private readonly FGACanvas innerPanel;
        private bool isSegmentsValid;

        public FGAArea()
        {
            innerPanel = new FGACanvas();
            Content = innerPanel;
            isSegmentsValid = false;
            SetCurrentValue(ForegroundProperty, Brushes.AliceBlue);
        }


        protected override Size MeasureOverride(Size constraint)
        {
            if(!isSegmentsValid) UpdateSegments();
            return base.MeasureOverride(constraint);
        }


        private void UpdateSegments()
        {
            innerPanel.Children.Clear();
            isSegmentsValid = true;
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
                LocateSegment(new Path { Fill = Foreground, StrokeThickness = 0, Stretch = Stretch.Fill, Data = leftGeometry,
                    Margin = new Thickness(innerMargin, innerMargin, 0, innerMargin) }, FloorId, minGroup, AreaId);

                // 중간 Segment 추가
                for(int groupIndex = minGroup + 1; groupIndex < maxGroup; groupIndex++)
                    LocateSegment(new Rectangle { Fill = Foreground, StrokeThickness = 0, Stretch = Stretch.Fill,
                        Margin = new Thickness(-1, innerMargin, -1, innerMargin) }, FloorId, groupIndex, AreaId);

                // Right Segment 추가
                LocateSegment(new Path { Fill = Foreground, StrokeThickness = 0, Stretch = Stretch.Fill, Data = rightGeometry,
                    Margin = new Thickness(0, innerMargin, innerMargin, innerMargin) }, FloorId, maxGroup, AreaId);
            }
        }


        private void LocateSegment(FrameworkElement fe, int floor, int group, int area)
        {
            fe.SetValue(FGACanvas.FloorProperty, floor);
            fe.SetValue(FGACanvas.GroupProperty, group);
            fe.SetValue(FGACanvas.AreaProperty, area);
            innerPanel.Children.Add(fe);
        }
    }

}
