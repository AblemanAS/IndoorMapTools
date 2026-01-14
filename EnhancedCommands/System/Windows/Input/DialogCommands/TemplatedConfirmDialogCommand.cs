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
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace EnhancedCommands.System.Windows.Input.DialogCommands
{
    public class TemplatedConfirmDialogCommand : TemplatedDialogCommand
    {
        [Bindable(true)]
        public ICommand OKCommand
        {
            get => (ICommand)GetValue(OKCommandProperty);
            set => SetValue(OKCommandProperty, value);
        }
        public static readonly DependencyProperty OKCommandProperty =
            DependencyProperty.Register(nameof(OKCommand), typeof(ICommand), typeof(TemplatedConfirmDialogCommand));

        [Bindable(true)]
        public object OKCommandParameter
        {
            get => GetValue(OKCommandParameterProperty);
            set => SetValue(OKCommandParameterProperty, value);
        }
        public static readonly DependencyProperty OKCommandParameterProperty =
            DependencyProperty.Register(nameof(OKCommandParameter), typeof(object), typeof(TemplatedConfirmDialogCommand));

        [Bindable(true)]
        public ICommand CancelCommand
        {
            get => (ICommand)GetValue(CancelCommandProperty);
            set => SetValue(CancelCommandProperty, value);
        }
        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(TemplatedConfirmDialogCommand));

        [Bindable(true)]
        public object CancelCommandParameter
        {
            get => GetValue(CancelCommandParameterProperty);
            set => SetValue(CancelCommandParameterProperty, value);
        }
        public static readonly DependencyProperty CancelCommandParameterProperty =
            DependencyProperty.Register(nameof(CancelCommandParameter), typeof(object), typeof(TemplatedConfirmDialogCommand));


        protected override void Open()
        {
            if(ContentTemplate == null) return;

            // 레이아웃 빌드
            var templatedPresenter = CreateTemplatedPresenter();
            var mainPanel = new StackPanel { Orientation = Orientation.Vertical };
            var confirmPanel = new StackPanel { Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(5) };
            (string strOK, string strCancel) = OkCancelTextProvider.Get(CultureInfo.CurrentUICulture);
            var okbtn = new Button      { Width = 70, Height = 25, Margin = new Thickness(5, 0, 0, 0), IsDefault = true, Content = strOK };
            var cancelbtn = new Button  { Width = 70, Height = 25, Margin = new Thickness(5, 0, 0, 0), IsCancel = true, Content = strCancel };
            confirmPanel.Children.Add(okbtn);
            confirmPanel.Children.Add(cancelbtn);
            mainPanel.Children.Add(templatedPresenter);
            mainPanel.Children.Add(confirmPanel);

            // 바인딩
            okbtn.SetBinding(Button.CommandProperty, new Binding(nameof(OKCommand)) { Source = this });
            okbtn.SetBinding(Button.CommandParameterProperty, new Binding(nameof(OKCommandParameter)) { Source = this });
            cancelbtn.SetBinding(Button.CommandProperty, new Binding(nameof(CancelCommand)) { Source = this });
            cancelbtn.SetBinding(Button.CommandParameterProperty, new Binding(nameof(CancelCommandParameter)) { Source = this });

            // 다이얼로그 생성 및 컨텐트, 컨텍스트 설정
            var dialog = CreateDialog();
            dialog.Content = mainPanel;

            // 확인버튼 다이얼로그 닫기 설정
            okbtn.Click += (sender, e) => dialog.DialogResult = true;

            OnOpening();
            dialog.ShowDialog();
        }

        protected override Freezable CreateInstanceCore() => new TemplatedConfirmDialogCommand();
    }
}
