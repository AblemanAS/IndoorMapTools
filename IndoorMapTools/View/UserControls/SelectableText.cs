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
using System.Windows.Markup;
using System.Windows.Media;

namespace IndoorMapTools.View.UserControls
{
    [ContentProperty(nameof(Items))]
    public class SelectableText : UserControl
    {
        public enum TextType { PlainText, HyperText }

        [Bindable(true)]
        public int SelectedValue { get => (int)GetValue(SelectedValueProperty); set => SetValue(SelectedValueProperty, value); }
        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register(nameof(SelectedValue), typeof(int), typeof(SelectableText),
                new FrameworkPropertyMetadata { BindsTwoWayByDefault = true });

        public string PrefixText { get; set; }
        public string PostfixText { get; set; }
        public TextType VisualType { get => visualType; set => Dispatcher.BeginInvoke(new Action(() => OnVisualTypeChanged(value))); }
        private TextType visualType;

        private void OnVisualTypeChanged(TextType value)
        {
            visualType = value;

            switch(value)
            {
                case TextType.PlainText:
                    mainTextBlock.InputBindings.Clear();
                    mainTextBlock.InputBindings.Add(new MouseBinding(MakeSelectableCommand, leftDoubleClickGesture));
                    break;

                case TextType.HyperText:
                    mainTextBlock.Foreground = blueBrush;
                    mainTextBlock.TextDecorations = TextDecorations.Underline;
                    mainTextBlock.Cursor = Cursors.Hand;
                    mainTextBlock.InputBindings.Clear();
                    mainTextBlock.InputBindings.Add(new MouseBinding(MakeSelectableCommand, leftClickGesture));
                    break;
            }
        }

        private ICommand MakeSelectableCommand { get; set; }
        public ItemCollection Items => comboBox.Items;
        public string DisplayMemberPath { get => comboBox.DisplayMemberPath; set => comboBox.DisplayMemberPath = value; }
        public string SelectedValuePath { get => comboBox.SelectedValuePath; set => comboBox.SelectedValuePath = value; }

        private static readonly SolidColorBrush blueBrush = new SolidColorBrush(Colors.Blue);
        private static readonly MouseGesture leftClickGesture = new MouseGesture(MouseAction.LeftClick);
        private static readonly MouseGesture leftDoubleClickGesture = new MouseGesture(MouseAction.LeftDoubleClick);

        private readonly TextBlock mainTextBlock;
        private readonly ComboBox comboBox;

        public SelectableText()
        {
            Grid mainGrid = new Grid();
            StackPanel spTextBlocks = new StackPanel { Orientation = Orientation.Horizontal };
            TextBlock textPrefix = new TextBlock();
            TextBlock textPostfix = new TextBlock();
            mainTextBlock = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            comboBox = new ComboBox();
            spTextBlocks.Children.Add(textPrefix);
            spTextBlocks.Children.Add(mainTextBlock);
            spTextBlocks.Children.Add(textPostfix);
            mainGrid.Children.Add(spTextBlocks);
            mainGrid.Children.Add(comboBox);

            // TextBlock StackPanel
            textPrefix.SetBinding(TextBlock.TextProperty, new Binding(nameof(PrefixText)) { Source = this });
            mainTextBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(comboBox.Text)) { Source = comboBox });
            textPostfix.SetBinding(TextBlock.TextProperty, new Binding(nameof(PostfixText)) { Source = this });
            VisualType = TextType.PlainText;

            // ComboBox
            comboBox.SetBinding(ComboBox.SelectedValueProperty, new Binding(nameof(SelectedValue)) { Source = this, Mode = BindingMode.TwoWay });
            comboBox.SetBinding(ComboBox.VisibilityProperty, new Binding(nameof(ComboBox.IsDropDownOpen))
            { Source = comboBox, Converter = new IsOpenToVisibilityConverter() });
            MakeSelectableCommand = new MakeSelectable(comboBox);
            Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(comboBox, (sender, e) => ExitSelectable());

            Content = mainGrid;
        }

        private void ExitSelectable()
        {
            comboBox.IsDropDownOpen = false;
            ReleaseMouseCapture();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                ExitSelectable();
                e.Handled = true;
            }
        }

        private class MakeSelectable : ICommand
        {
            private readonly ComboBox comboBox;

            public MakeSelectable(ComboBox comboBox) => this.comboBox = comboBox;

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public bool CanExecute(object parameter) => true;
            public void Execute(object parameter)
            {
                Mouse.Capture(comboBox);
                comboBox.IsDropDownOpen = true;
            }
        }

        private class IsOpenToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                => (value is bool isOpen && isOpen) ? Visibility.Visible : Visibility.Hidden;

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                => throw new NotImplementedException();
        }
    }
}
