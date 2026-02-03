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
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IndoorMapTools.View.UserControls
{
    public class DragBoxSupport : Canvas
    {
        private const int DRAGBOX_MARGIN = 1;

        private Point? DragStartMousePos
        {
            get => (Point?)GetValue(DragStartMousePosProperty);
            set => SetValue(DragStartMousePosProperty, value);
        }
        private static readonly DependencyProperty DragStartMousePosProperty =
            DependencyProperty.Register(nameof(DragStartMousePos), typeof(Point?), typeof(DragBoxSupport));

        private Point CurrentMousePos
        {
            get => (Point)GetValue(CurrentMousePosProperty);
            set => SetValue(CurrentMousePosProperty, value);
        }
        private static readonly DependencyProperty CurrentMousePosProperty =
            DependencyProperty.Register(nameof(CurrentMousePos), typeof(Point), typeof(DragBoxSupport), new PropertyMetadata(OnCurPosChanged));

        private static void OnCurPosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is DragBoxSupport instance && instance.DragStartMousePos is Point startPos && e.NewValue is Point curPos)
            {
                double left, top, right, bottom;
                if(startPos.X < curPos.X) { left = startPos.X; right = curPos.X; }
                else { left = curPos.X; right = startPos.X; }
                if(startPos.Y < curPos.Y) { top = startPos.Y; bottom = curPos.Y; }
                else { top = curPos.Y; bottom = startPos.Y; }

                left = Math.Max(left, 0);
                top = Math.Max(top, 0);
                right = Math.Min(right, instance.ActualWidth - 1);
                bottom = Math.Min(bottom, instance.ActualHeight - 1);

                instance.dragRect.X = left;
                instance.dragRect.Y = top;
                instance.dragRect.Width = right - left;
                instance.dragRect.Height = bottom - top;

                Canvas.SetLeft(instance.Visual, left);
                Canvas.SetTop(instance.Visual, top);
                instance.Visual.Width = instance.dragRect.Width;
                instance.Visual.Height = instance.dragRect.Height;

            }
        }

        private Rect dragRect;
        private readonly Rectangle Visual = new Rectangle
        { Stroke = Brushes.Black, StrokeThickness = 1, StrokeDashArray = { 2, 2 }, Fill = Brushes.Transparent };

        public delegate void DragBoxHandler(Rect dragRect);
        public event DragBoxHandler Finished;
        public new event DragBoxHandler SizeChanged;

        public DragBoxSupport()
        {
            IsHitTestVisible = false;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Background = Brushes.Transparent;
            Children.Add(Visual);

            // Visibility 바인딩
            Binding bindingStartPos = new Binding(nameof(DragStartMousePos)) { Source = this };
            Binding bindingCurPos = new Binding(nameof(CurrentMousePos)) { Source = this };
            Visual.SetBinding(Rectangle.VisibilityProperty, new MultiBinding
            {
                Bindings = { bindingStartPos, bindingCurPos },
                Converter = new VisibilitySetter()
            });
        }

        public void OnParentMouseButtonDown(object sender, MouseButtonEventArgs e)
            => SetValue(DragStartMousePosProperty, e.GetPosition(this));

        public void OnParentMouseMove(object sender, MouseEventArgs e)
        {
            if(DragStartMousePos != null)
                SetValue(CurrentMousePosProperty, e.GetPosition(this));
            SizeChanged?.Invoke(dragRect);
        }

        public void OnParentMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            SetValue(DragStartMousePosProperty, null);
            Finished?.Invoke(dragRect);
            Canvas.SetLeft(Visual, 0);
            Canvas.SetTop(Visual, 0);
            Visual.Width = 0;
            Visual.Height = 0;
        }

        class VisibilitySetter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
                => (values[0] is Point && values[1] is Point) ? Visibility.Visible : Visibility.Hidden;

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
                => throw new NotImplementedException();
        }
    }
}
