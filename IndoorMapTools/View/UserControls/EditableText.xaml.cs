using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace IndoorMapTools.View.UserControls
{
    public class EditableText : Panel
    {
        public enum TextType { PlainText, HyperText }
        public enum ValidationType { None, Name, Natural, Int, Double, PositiveDouble }

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
        public string AlternativeText
        {
            get => (string)GetValue(AlternativeTextProperty);
            set => SetValue(AlternativeTextProperty, value);
        }
        public static readonly DependencyProperty AlternativeTextProperty =
            DependencyProperty.Register(nameof(AlternativeText), typeof(string), typeof(EditableText));

        [Bindable(true)]
        public bool ShowAlternativeText
        {
            get => (bool)GetValue(ShowAlternativeTextProperty);
            set => SetValue(ShowAlternativeTextProperty, value);
        }
        public static readonly DependencyProperty ShowAlternativeTextProperty =
            DependencyProperty.Register(nameof(ShowAlternativeText), typeof(bool), typeof(EditableText),
                new FrameworkPropertyMetadata(OnShowAlternativeTextChanged));

        private static void OnShowAlternativeTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is EditableText instance)) return;
            instance.mainTextBlock.SetBinding(TextBlock.TextProperty,
                new Binding((e.NewValue is bool showAlternativeText && showAlternativeText) ? nameof(AlternativeText) : nameof(Value))
                { Source = instance });
        }

        [Bindable(true)]
        public string PrefixText
        {
            get => (string)GetValue(PrefixTextProperty);
            set => SetValue(PrefixTextProperty, value);
        }
        public static readonly DependencyProperty PrefixTextProperty =
            DependencyProperty.Register(nameof(PrefixText), typeof(string), typeof(EditableText),
                new FrameworkPropertyMetadata { BindsTwoWayByDefault = true });

        [Bindable(true)]
        public string PostfixText
        {
            get => (string)GetValue(PostfixTextProperty);
            set => SetValue(PostfixTextProperty, value);
        }
        public static readonly DependencyProperty PostfixTextProperty =
            DependencyProperty.Register(nameof(PostfixText), typeof(string), typeof(EditableText),
                new FrameworkPropertyMetadata { BindsTwoWayByDefault = true });

        public ValidationType Validator { get; set; } = ValidationType.None;
        public TextType VisualType { get; set; } = TextType.PlainText;
        private readonly ICommand makeEditableCommand;

        private static readonly MouseGesture leftClickGesture = new MouseGesture(MouseAction.LeftClick);
        private static readonly MouseGesture leftDoubleClickGesture = new MouseGesture(MouseAction.LeftDoubleClick);
        private static readonly char[] ACCEPTABLE_SYMBOLS_ENTITY_NAME = { '_', '-', '[', ']', '(', ')', '{', '}' };

        private readonly TextBlock mainTextBlock;
        private readonly TextBlock prefixTextBlock;
        private readonly TextBlock postfixTextBlock;
        private readonly TextBox textBox;

        public EditableText()
        {
            mainTextBlock = new TextBlock();
            prefixTextBlock = new TextBlock();
            postfixTextBlock = new TextBlock();
            Children.Add(mainTextBlock);
            Children.Add(prefixTextBlock);
            Children.Add(postfixTextBlock);

            mainTextBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(Value)) { Source = this });
            prefixTextBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(PrefixText)) { Source = this });
            postfixTextBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(PostfixText)) { Source = this });

            // TextBox
            textBox = new TextBox
            {
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
                MinWidth = 20
            };
            Children.Add(textBox);

            textBox.LostKeyboardFocus += (sender, e) => Mouse.Capture(null);
            textBox.PreviewTextInput += FilterInputText;

            textBox.Style = new Style(typeof(TextBox))
            {
                Setters = { new Setter(VisibilityProperty, Visibility.Hidden) },
                Triggers = { new Trigger { Property = IsFocusedProperty, Value = true,
                    Setters = { new Setter(VisibilityProperty, Visibility.Visible) } } }
            };

            textBox.LostFocus += (sender, e) =>
            {
                if(TextIsValid(textBox.Text)) SetCurrentValue(EditableText.ValueProperty, textBox.Text);
                else textBox.Text = Value.ToString();
            };

            makeEditableCommand = new MakeEditable(this);
            Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(this, (sender, e) => ReleaseFocus());
            Loaded += OnLoaded;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            textBox.Measure(constraint);
            mainTextBlock.Measure(constraint);
            prefixTextBlock.Measure(constraint);
            postfixTextBlock.Measure(constraint);
            double textBlockTotalWidth = prefixTextBlock.DesiredSize.Width + mainTextBlock.DesiredSize.Width + postfixTextBlock.DesiredSize.Width;
            return new Size(Math.Max(textBox.DesiredSize.Width, textBlockTotalWidth), textBox.DesiredSize.Height);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            textBox.Arrange(new Rect(default, arrangeSize));
            double textBlockOffset = 0;
            prefixTextBlock.Arrange(new Rect(default, arrangeSize));
            textBlockOffset += prefixTextBlock.DesiredSize.Width;
            mainTextBlock.Arrange(new Rect(new Point(textBlockOffset, 0), arrangeSize));
            textBlockOffset += mainTextBlock.DesiredSize.Width;
            postfixTextBlock.Arrange(new Rect(new Point(textBlockOffset, 0), arrangeSize));
            return arrangeSize;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            switch(VisualType)
            {
                case TextType.PlainText:
                    mainTextBlock.Style = new Style(typeof(TextBlock));
                    mainTextBlock.InputBindings.Add(new MouseBinding(makeEditableCommand, leftDoubleClickGesture));
                    break;

                case TextType.HyperText:
                    mainTextBlock.Style = new Style(typeof(TextBlock))
                    {
                        Setters = { new Setter { Property = TextBlock.ForegroundProperty, Value = Brushes.Blue } },
                        Triggers = { new Trigger { Property = TextBlock.IsEnabledProperty, Value = false,
                            Setters = { new Setter { Property = TextBlock.ForegroundProperty, Value = Brushes.Gray }}}}
                    };
                    mainTextBlock.TextDecorations = TextDecorations.Underline;
                    mainTextBlock.Cursor = Cursors.Hand;
                    mainTextBlock.InputBindings.Add(new MouseBinding(makeEditableCommand, leftClickGesture));
                    break;
            }

            Loaded -= OnLoaded;
        }

        private void ReleaseFocus()
        {
            Keyboard.ClearFocus();
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(textBox), null);
            ReleaseMouseCapture();
        }

        private void FilterInputText(object sender, TextCompositionEventArgs e)
        {
            switch(Validator)
            {
                case ValidationType.Name:
                    foreach(char ch in e.Text)
                        if(!(char.IsLetterOrDigit(ch) || ACCEPTABLE_SYMBOLS_ENTITY_NAME.Contains(ch)))
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

                case ValidationType.Double:
                    foreach(char ch in e.Text)
                        if(!char.IsDigit(ch) && !(ch == '.') && !(ch == '-'))
                            e.Handled = true;
                    break;

                case ValidationType.PositiveDouble:
                    foreach(char ch in e.Text)
                        if(!char.IsDigit(ch) && !(ch == '.'))
                            e.Handled = true;
                    break;
            }
        }


        private bool TextIsValid(string text)
        {
            string stripped = text.Replace(" ", "");
            if(stripped.Length == 0) return false;

            switch(Validator)
            {
                case ValidationType.Name:
                    foreach(char ch in text)
                        if(!(char.IsLetterOrDigit(ch) || ACCEPTABLE_SYMBOLS_ENTITY_NAME.Contains(ch)))
                            return false;
                    return true;

                case ValidationType.Natural:
                    return int.TryParse(text, out int resNatural) && !(resNatural < 0);

                case ValidationType.Int:
                    return int.TryParse(text, out _);

                case ValidationType.Double:
                    return double.TryParse(text, out _);

                case ValidationType.PositiveDouble:
                    return double.TryParse(text, out double resDouble) && !(resDouble < 0.0);

                default:
                    return true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Enter:
                    ReleaseFocus();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    textBox.Text = Value.ToString();
                    ReleaseFocus();
                    e.Handled = true;
                    break;
            }
        }

        private class MakeEditable : ICommand
        {
            private readonly EditableText control;

            public MakeEditable(EditableText control) => this.control = control;

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter)
            {
                Mouse.Capture(control);
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(control.textBox), control.textBox);
                control.textBox.Text = control.Value?.ToString();
                control.textBox.SelectAll();
            }
        }
    }
}
