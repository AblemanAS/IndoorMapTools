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
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
namespace MapView.System.Windows.Controls
{
    // 맵뷰 내 마우스 툴 정의
    public class MouseTool : FrameworkElement
    {
        public string ToolName { get; set; } = "";
        public Key ActivationKey { get; set; } = Key.None;  // 활성화 키
        public bool DragBoxEnabled { get; set; } = false;   // 드래그 시 드래그박스 그리는지 여부

        [Bindable(true)]
        public Cursor DefaultCursor // 기본 커서 모양
        {
            get => (Cursor)GetValue(DefaultCursorProperty);
            set => SetValue(DefaultCursorProperty, value);
        }
        public static readonly DependencyProperty DefaultCursorProperty =
            DependencyProperty.Register(nameof(DefaultCursor), typeof(Cursor), typeof(MouseTool),
                new FrameworkPropertyMetadata(Cursors.Arrow));

        [Bindable(true)]
        public Cursor ClickedCursor // 클릭했을 때 커서 모양
        {
            get => (Cursor)GetValue(ClickedCursorProperty);
            set => SetValue(ClickedCursorProperty, value);
        }
        public static readonly DependencyProperty ClickedCursorProperty =
            DependencyProperty.Register(nameof(ClickedCursor), typeof(Cursor), typeof(MouseTool));

        [Bindable(true)]
        public new ICommand OnMouseMove // 마우스 이동 시
        {
            get => (ICommand)GetValue(OnMouseMoveProperty);
            set => SetValue(OnMouseMoveProperty, value);
        }
        public static readonly DependencyProperty OnMouseMoveProperty =
            DependencyProperty.Register(nameof(OnMouseMove), typeof(ICommand), typeof(MouseTool));

        [Bindable(true)]
        public ICommand OnMouseLeftDown // 마우스 좌클릭 Down 시
        {
            get => (ICommand)GetValue(OnMouseLeftDownProperty);
            set => SetValue(OnMouseLeftDownProperty, value);
        }
        public static readonly DependencyProperty OnMouseLeftDownProperty =
            DependencyProperty.Register(nameof(OnMouseLeftDown), typeof(ICommand), typeof(MouseTool));

        [Bindable(true)]
        public ICommand OnMouseLeftUp // 마우스 좌클릭 Up 시
        {
            get => (ICommand)GetValue(OnMouseLeftUpProperty);
            set => SetValue(OnMouseLeftUpProperty, value);
        }
        public static readonly DependencyProperty OnMouseLeftUpProperty =
            DependencyProperty.Register(nameof(OnMouseLeftUp), typeof(ICommand), typeof(MouseTool));

        [Bindable(true)]
        public ICommand OnMouseRightDown // 마우스 우클릭 Down 시
        {
            get => (ICommand)GetValue(OnMouseRightDownProperty);
            set => SetValue(OnMouseRightDownProperty, value);
        }
        public static readonly DependencyProperty OnMouseRightDownProperty =
            DependencyProperty.Register(nameof(OnMouseRightDown), typeof(ICommand), typeof(MouseTool));

        [Bindable(true)]
        public ICommand OnMouseRightUp // 마우스 우클릭 Up 시
        {
            get => (ICommand)GetValue(OnMouseRightUpProperty);
            set => SetValue(OnMouseRightUpProperty, value);
        }
        public static readonly DependencyProperty OnMouseRightUpProperty =
            DependencyProperty.Register(nameof(OnMouseRightUp), typeof(ICommand), typeof(MouseTool));

        [Bindable(true)]
        public ICommand OnMouseLeftDrag // 마우스 좌버튼 드래그 시
        {
            get => (ICommand)GetValue(OnMouseLeftDragProperty);
            set => SetValue(OnMouseLeftDragProperty, value);
        }
        public static readonly DependencyProperty OnMouseLeftDragProperty =
            DependencyProperty.Register(nameof(OnMouseLeftDrag), typeof(ICommand), typeof(MouseTool));

        [Bindable(true)]
        public ICommand OnMouseLeftDoubleClick // 마우스 좌버튼 더블클릭 시
        {
            get => (ICommand)GetValue(OnMouseLeftDoubleClickProperty);
            set => SetValue(OnMouseLeftDoubleClickProperty, value);
        }
        public static readonly DependencyProperty OnMouseLeftDoubleClickProperty =
            DependencyProperty.Register(nameof(OnMouseLeftDoubleClick), typeof(ICommand), typeof(MouseTool));

        [Bindable(true)]
        public ICommand OnMouseXButton1Down // 마우스 Back 버튼 Down 시
        {
            get => (ICommand)GetValue(OnMouseXButton1DownProperty);
            set => SetValue(OnMouseXButton1DownProperty, value);
        }
        public static readonly DependencyProperty OnMouseXButton1DownProperty =
            DependencyProperty.Register(nameof(OnMouseXButton1Down), typeof(ICommand), typeof(MouseTool));

        [Bindable(true)]
        public ICommand OnMouseXButton1Up // 마우스 Back 버튼 Up 시
        {
            get => (ICommand)GetValue(OnMouseXButton1UpProperty);
            set => SetValue(OnMouseXButton1UpProperty, value);
        }
        public static readonly DependencyProperty OnMouseXButton1UpProperty =
            DependencyProperty.Register(nameof(OnMouseXButton1Up), typeof(ICommand), typeof(MouseTool));


        [Bindable(true)]
        public ICommand OnMouseXButton2Down // 마우스 Forward 버튼 Down 시
        {
            get => (ICommand)GetValue(OnMouseXButton2DownProperty);
            set => SetValue(OnMouseXButton2DownProperty, value);
        }
        public static readonly DependencyProperty OnMouseXButton2DownProperty =
            DependencyProperty.Register(nameof(OnMouseXButton2Down), typeof(ICommand), typeof(MouseTool));

        [Bindable(true)]
        public ICommand OnMouseXButton2Up // 마우스 Forward 버튼 Up 시
        {
            get => (ICommand)GetValue(OnMouseXButton2UpProperty);
            set => SetValue(OnMouseXButton2UpProperty, value);
        }
        public static readonly DependencyProperty OnMouseXButton2UpProperty =
            DependencyProperty.Register(nameof(OnMouseXButton2Up), typeof(ICommand), typeof(MouseTool));
    }

    public struct MouseToolEventArgs
    {
        public Point Position;  // 이미지 기준 클릭 좌표
        public Rect DragBox;    // 이미지 기준 드래그 박스 좌표 (DragBoxEnabled = true 일 때만 올바른 값이 들어옴)
    }
}
