using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace MapView.System.Windows.Controls
{
    public class PolygonTool : MouseTool
    {
        [Bindable(true)]
        public Point[] PolygonSource // 뷰모델 폴리곤
        {
            get => (Point[])GetValue(PolygonSourceProperty);
            set => SetValue(PolygonSourceProperty, value);
        }
        public static readonly DependencyProperty PolygonSourceProperty =
            DependencyProperty.Register(nameof(PolygonSource), typeof(Point[]), typeof(PolygonTool),
                new FrameworkPropertyMetadata(OnPolygonSourceChanged) { BindsTwoWayByDefault = true });

        private static void OnPolygonSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is PolygonTool instance)) return;
            instance.PolygonVisual.Clear();
            if(e.NewValue != null)
                Array.ForEach(instance.PolygonSource, e => instance.PolygonVisual.Add(e));
        }


        [Bindable(true)]
        public ObservableCollection<Point> PolygonVisual // 뷰모델 폴리곤
        {
            get => (ObservableCollection<Point>)GetValue(PolygonVisualProperty);
            set => SetValue(PolygonVisualProperty, value);
        }
        public static readonly DependencyProperty PolygonVisualProperty =
            DependencyProperty.Register(nameof(PolygonVisual), typeof(ObservableCollection<Point>), typeof(PolygonTool),
                new FrameworkPropertyMetadata { BindsTwoWayByDefault = true, DefaultValue = new ObservableCollection<Point>()});


        [Bindable(true)]
        public bool IsDrawing // 편집 중
        {
            get => (bool)GetValue(IsDrawingProperty);
            set => SetValue(IsDrawingProperty, value);
        }
        public static readonly DependencyProperty IsDrawingProperty =
            DependencyProperty.Register(nameof(IsDrawing), typeof(bool), typeof(PolygonTool),
                new FrameworkPropertyMetadata(OnIsDrawingChanged));


        private static void OnIsDrawingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is PolygonTool instance && e.NewValue is bool newval)) return;
            if(newval) instance.StartDrawing();
            else instance.EndDrawing();
        }


        public PolygonTool()
        {
            DefaultCursor = Cursors.Cross;
            OnMouseMove = new MouseToolCommand(DrawCurrent);
            OnMouseLeftDown = new MouseToolCommand((e) => PolygonVisual.Add(e.Position));
            OnMouseRightDown = new MouseToolCommand((e) => SetValue(IsDrawingProperty, false));
            OnMouseXButton1Down = new MouseToolCommand(DeductCurrent);
        }

        private class MouseToolCommand : ICommand
        {
            private readonly Action<MouseToolEventArgs> _execute;

            public MouseToolCommand(Action<MouseToolEventArgs> execute)
                 => _execute = execute;

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            public bool CanExecute(object parameter) => parameter is MouseToolEventArgs;

            public void Execute(object parameter)
            {
                if(parameter is MouseToolEventArgs args)
                    _execute?.Invoke(args);
            }
        }


        /*** Drawing Logics ***/

        private void StartDrawing()
        {
            // 시작점 추가
            if(PolygonVisual.Count == 0) PolygonVisual.Add(new Point());
            else if(PolygonVisual.Count == 2) PolygonVisual.RemoveAt(1);
            else if(PolygonVisual.Count > 2) PolygonVisual.Add(PolygonVisual[0]);
        }

        private void DrawCurrent(MouseToolEventArgs e)
        {
            if(!IsDrawing) return;

            Point targetPos = e.Position;

            if(PolygonVisual.Count > 1 && Keyboard.IsKeyDown(Key.LeftShift))
            {
                double xVector = Math.Abs(targetPos.X - PolygonVisual[PolygonVisual.Count - 2].X);
                double yVector = Math.Abs(targetPos.Y - PolygonVisual[PolygonVisual.Count - 2].Y);
                if(xVector > yVector) targetPos.Y = PolygonVisual[PolygonVisual.Count - 2].Y;
                else targetPos.X = PolygonVisual[PolygonVisual.Count - 2].X;
            }

            PolygonVisual[PolygonVisual.Count - 1] = targetPos;
        }

        private void DeductCurrent(MouseToolEventArgs e)
        {
            if(!IsDrawing) return;
            if(PolygonVisual.Count > 1)
                PolygonVisual.RemoveAt(PolygonVisual.Count - 1);

            DrawCurrent(e);
        }

        private void EndDrawing()
        {
            if(PolygonVisual.Count < 4)
            {
                PolygonVisual.Clear();
                Array.ForEach(PolygonSource, e => PolygonVisual.Add(e));
            }
            else
            {
                PolygonVisual.RemoveAt(PolygonVisual.Count - 1);
                var updatedPolygon = new Point[PolygonVisual.Count];
                PolygonVisual.CopyTo(updatedPolygon, 0);
                PolygonSource = updatedPolygon;
            }
        }
    }
}
