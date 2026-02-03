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
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace IndoorMapTools.View.UserControls
{
    public class EditableText : Panel
    {
        private static readonly MouseGesture leftClickGesture = new MouseGesture(MouseAction.LeftClick);
        private static readonly char[] ACCEPTABLE_SYMBOLS_ENTITY_NAME = { '_', '-', '[', ']', '(', ')', '{', '}' };

        [Bindable(true)]
        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(object), typeof(EditableText),
                new FrameworkPropertyMetadata { BindsTwoWayByDefault = true });

        [Bindable(true)]
        public string Prefix
        {
            get => (string)GetValue(PrefixProperty);
            set => SetValue(PrefixProperty, value);
        }
        public static readonly DependencyProperty PrefixProperty =
            DependencyProperty.Register(nameof(Prefix), typeof(string), typeof(EditableText));

        [Bindable(true)]
        public string Postfix
        {
            get => (string)GetValue(PostfixProperty);
            set => SetValue(PostfixProperty, value);
        }
        public static readonly DependencyProperty PostfixProperty =
            DependencyProperty.Register(nameof(Postfix), typeof(string), typeof(EditableText));

        [Bindable(true)]
        public string NullDisplayText
        {
            get => (string)GetValue(NullDisplayTextProperty);
            set => SetValue(NullDisplayTextProperty, value);
        }
        public static readonly DependencyProperty NullDisplayTextProperty =
            DependencyProperty.Register(nameof(NullDisplayText), typeof(string), typeof(EditableText));



        public enum ValidationType { None, Name, Int, Natural, Float, PositiveFloat, Double, PositiveDouble }
        public ValidationType Validator { get; set; } = ValidationType.None;

        private readonly TextBlock mainTextBlock;
        private readonly TextBlock prefixTextBlock;
        private readonly TextBlock postfixTextBlock;
        private readonly TextBox textBox;
        private bool IsInEditMode = false;

        public EditableText()
        {
            // 접두, 접미 TextBlock
            prefixTextBlock = new TextBlock();
            postfixTextBlock = new TextBlock();
            prefixTextBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(Prefix)) { Source = this });
            postfixTextBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(Postfix)) { Source = this });

            // 주 TextBlock
            mainTextBlock = new TextBlock
            {
                TextDecorations = TextDecorations.Underline,
                Cursor = Cursors.Hand,
                Style = new Style(typeof(TextBlock))
                {
                    Setters = { new Setter(TextBlock.TextProperty, new Binding(nameof(Value)) { Source = this }),
                                new Setter(TextBlock.ForegroundProperty, Brushes.Blue) },
                    Triggers = { new Trigger { Property = IsEnabledProperty, Value = false,
                                               Setters = { new Setter(TextBlock.ForegroundProperty, Brushes.Gray) }},
                                 new DataTrigger { Binding = new Binding(nameof(Value)) { Source = this }, Value = null,
                                                   Setters = { new Setter(TextBlock.TextProperty, new Binding(nameof(NullDisplayText)) { Source = this }) }}}
                }
            };

            // 값 편집 TextBox
            textBox = new TextBox
            {
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
                MinWidth = 20,
                Visibility = Visibility.Hidden,
                IsTabStop = false,
                Focusable = false
            };

            // 트리 구성
            Children.Add(prefixTextBlock);
            Children.Add(postfixTextBlock);
            Children.Add(mainTextBlock);
            Children.Add(textBox);

            // 이벤트 연결
            textBox.PreviewTextInput += FilterInputText;
            textBox.LostKeyboardFocus += (sender, e) => EndEdit();

            // 주 TextBlock 클릭 시 편집 모드 시작
            mainTextBlock.InputBindings.Add(new MouseBinding(new SimpleCommand(StartEdit), leftClickGesture));
            // 편집 모드에서 외부 클릭 시 편집 종료 (해당 클릭 이벤트는 consume해서 외부 이벤트 억제)
            Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(this, (sender, e) => { EndEdit(); e.Handled = true; });
        }


        // 편집 모드 시작
        private void StartEdit()
        {
            if(IsInEditMode) return;
            IsInEditMode = true;

            textBox.Focusable = true;
            mainTextBlock.Visibility = Visibility.Hidden;
            textBox.Visibility = Visibility.Visible;
            Mouse.Capture(this);
            Keyboard.Focus(textBox);
            textBox.Text = Value?.ToString();
            textBox.SelectAll();
        }


        // 편집 모드 종료
        private void EndEdit(bool commit = true)
        {
            if(!IsInEditMode) return;
            IsInEditMode = false;

            // 마우스 캡쳐 및 키보드 포커스 제거
            Mouse.Capture(null);
            Keyboard.ClearFocus();

            // 커밋 모드일 경우, 유효한 텍스트일 경우에만 값 Value로 커밋
            if(commit && TryConvertTextToValueType(textBox.Text, out object converted))
                SetCurrentValue(EditableText.ValueProperty, converted);

            // 텍스트박스 공란 및 숨기기
            textBox.Text = string.Empty;
            textBox.Visibility = Visibility.Hidden;
            mainTextBlock.Visibility = Visibility.Visible;
            textBox.Focusable = false;
        }


        private bool TryConvertTextToValueType(string text, out object converted)
        {
            object tmp = null;

            bool proc<T>(bool success, T value)
            {
                if(!success) return false;
                tmp = value;
                return true;
            }

            bool pf = Validator switch
            {
                ValidationType.None =>              proc(true, text),
                ValidationType.Name =>              proc(true, text),
                ValidationType.Int =>               proc(int.TryParse(text, out var resI), resI),
                ValidationType.Natural =>           proc(int.TryParse(text, out var resN) && resN >= 0, resN),
                ValidationType.Float =>             proc(float.TryParse(text, out var resF), resF),
                ValidationType.PositiveFloat =>     proc(float.TryParse(text, out var resPF) && resPF >= 0f, resPF),
                ValidationType.Double =>            proc(double.TryParse(text, out var resD), resD),
                ValidationType.PositiveDouble =>    proc(double.TryParse(text, out var resPD) && resPD >= 0.0, resPD),
                _ => false
            };

            converted = tmp;
            return pf;
        }


        protected override void OnKeyDown(KeyEventArgs e)
        {
            if(!IsInEditMode) return;

            switch(e.Key)
            {
                case Key.Enter:
                    EndEdit();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    textBox.Text = Value?.ToString();
                    EndEdit(commit: false);
                    e.Handled = true;
                    break;
            }
        }


        protected override Size MeasureOverride(Size constraint)
        {
            foreach(UIElement child in Children) child?.Measure(constraint);
            double textBlockTotalWidth = prefixTextBlock.DesiredSize.Width + mainTextBlock.DesiredSize.Width + postfixTextBlock.DesiredSize.Width;
            return new Size(Math.Max(textBox.DesiredSize.Width, textBlockTotalWidth), textBox.DesiredSize.Height); // 높이는 TextBox 기준 의도
        }


        protected override Size ArrangeOverride(Size arrangeSize)
        {
            textBox.Arrange(new Rect(default, arrangeSize));

            double textBlockOffset = 0;

            double prefixTextBlockWidth = prefixTextBlock.DesiredSize.Width;
            prefixTextBlock.Arrange(new Rect(textBlockOffset, 0, prefixTextBlockWidth, arrangeSize.Height));
            textBlockOffset += prefixTextBlockWidth;

            double mainTextBlockWidth = mainTextBlock.DesiredSize.Width;
            mainTextBlock.Arrange(new Rect(textBlockOffset, 0, mainTextBlockWidth, arrangeSize.Height));
            textBlockOffset += mainTextBlockWidth;

            double postfixTextBlockWidth = postfixTextBlock.DesiredSize.Width;
            postfixTextBlock.Arrange(new Rect(textBlockOffset, 0, postfixTextBlockWidth, arrangeSize.Height));

            return arrangeSize;
        }


        private void FilterInputText(object sender, TextCompositionEventArgs e)
        {
            switch(Validator)
            {
                case ValidationType.Name:
                    foreach(char ch in e.Text)
                        if(!(char.IsLetterOrDigit(ch) || IsAcceptableSymbol(ch)))
                            e.Handled = true;
                    break;

                case ValidationType.Natural:
                    foreach(char ch in e.Text)
                        if(!char.IsDigit(ch))
                            e.Handled = true;
                    break;

                case ValidationType.Int:
                    foreach(char ch in e.Text)
                        if(!char.IsDigit(ch) && !(ch == '-'))
                            e.Handled = true;
                    break;

                case ValidationType.Float:
                case ValidationType.Double:
                    foreach(char ch in e.Text)
                        if(!char.IsDigit(ch) && !(ch == '.') && !(ch == '-'))
                            e.Handled = true;
                    break;

                case ValidationType.PositiveFloat:
                case ValidationType.PositiveDouble:
                    foreach(char ch in e.Text)
                        if(!char.IsDigit(ch) && !(ch == '.'))
                            e.Handled = true;
                    break;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsAcceptableSymbol(char ch)
        {
            for(int i = 0; i < ACCEPTABLE_SYMBOLS_ENTITY_NAME.Length; i++)
                if(ACCEPTABLE_SYMBOLS_ENTITY_NAME[i] == ch) return true;
            return false;
        }

        private sealed class SimpleCommand : ICommand
        {
            private readonly Action execute;
            public SimpleCommand(Action execute) => this.execute = execute;
            public event EventHandler CanExecuteChanged { add {} remove {} }
            public bool CanExecute(object parameter) => true;
            public void Execute(object parameter) => execute();
        }
    }
}