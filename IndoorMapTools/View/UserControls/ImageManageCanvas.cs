using IndoorMapTools.Helper;
using System;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.View.UserControls
{
    public class ImageManageCanvas : Canvas
    {
        [Bindable(true)]
        public BitmapImage OriginalSource
        {
            get => (BitmapImage)GetValue(OriginalSourceProperty);
            set => SetValue(OriginalSourceProperty, value);
        }
        public static readonly DependencyProperty OriginalSourceProperty =
            DependencyProperty.Register(nameof(OriginalSource), typeof(BitmapImage), typeof(ImageManageCanvas),
                new FrameworkPropertyMetadata() { BindsTwoWayByDefault = true });

        [Bindable(true)]
        public BitmapImage AlternativeSource
        {
            get => (BitmapImage)GetValue(AlternativeSourceProperty);
            set => SetValue(AlternativeSourceProperty, value);
        }
        public static readonly DependencyProperty AlternativeSourceProperty =
            DependencyProperty.Register(nameof(AlternativeSource), typeof(BitmapImage), typeof(ImageManageCanvas),
                new FrameworkPropertyMetadata(AlternativeSourceChanged));

        private static void AlternativeSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(!(d is ImageManageCanvas instance)) return;
            if(e.NewValue != null) instance.innerBorderAlternative.SetValue(VisibilityProperty, Visibility.Visible);
            else instance.innerBorderAlternative.SetValue(VisibilityProperty, Visibility.Hidden);
        }

        [Bindable(true)]
        public int LeftPad
        { get => (int)GetValue(LeftPadProperty); set => SetValue(LeftPadProperty, value); }
        public static readonly DependencyProperty LeftPadProperty =
            DependencyProperty.Register(nameof(LeftPad), typeof(int), typeof(ImageManageCanvas),
                new FrameworkPropertyMetadata(OnPadValueChanged));

        [Bindable(true)]
        public int TopPad
        { get => (int)GetValue(TopPadProperty); set => SetValue(TopPadProperty, value); }
        public static readonly DependencyProperty TopPadProperty =
            DependencyProperty.Register(nameof(TopPad), typeof(int), typeof(ImageManageCanvas),
                new FrameworkPropertyMetadata(OnPadValueChanged));

        [Bindable(true)]
        public int RightPad
        { get => (int)GetValue(RightPadProperty); set => SetValue(RightPadProperty, value); }
        public static readonly DependencyProperty RightPadProperty =
            DependencyProperty.Register(nameof(RightPad), typeof(int), typeof(ImageManageCanvas),
                new FrameworkPropertyMetadata(OnPadValueChanged));

        [Bindable(true)]
        public int BottomPad
        { get => (int)GetValue(BottomPadProperty); set => SetValue(BottomPadProperty, value); }
        public static readonly DependencyProperty BottomPadProperty =
            DependencyProperty.Register(nameof(BottomPad), typeof(int), typeof(ImageManageCanvas),
                new FrameworkPropertyMetadata(OnPadValueChanged));

        private static void OnPadValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as ImageManageCanvas)?.SetValue(ImageManageCanvas.AlternativeSourceProperty, null);

        [Bindable(true)]
        public Size? TargetSize
        { get => (Size?)GetValue(TargetSizeProperty); set => SetValue(TargetSizeProperty, value); }
        public static readonly DependencyProperty TargetSizeProperty =
            DependencyProperty.Register(nameof(TargetSize), typeof(Size?), typeof(ImageManageCanvas));

        private readonly Border innerBorderAlternative;

        public ImageManageCanvas()
        {
            // 기본값 설정
            AllowDrop = true;
            ClipToBounds = true;
            Background = Brushes.White;
            Margin = new Thickness(-1);

            // 크기 바인딩
            var bindingWidth = new MultiBinding { Converter = new SumCalculator() };
            bindingWidth.Bindings.Add(new Binding(nameof(LeftPad)) { Source = this });
            bindingWidth.Bindings.Add(new Binding(nameof(OriginalSource) + ".PixelWidth") { Source = this, FallbackValue = 50.0 });
            bindingWidth.Bindings.Add(new Binding(nameof(RightPad)) { Source = this });
            SetBinding(WidthProperty, bindingWidth);

            var bindingHeight = new MultiBinding { Converter = new SumCalculator() };
            bindingHeight.Bindings.Add(new Binding(nameof(TopPad)) { Source = this });
            bindingHeight.Bindings.Add(new Binding(nameof(OriginalSource) + ".PixelHeight") { Source = this, FallbackValue = 50.0 });
            bindingHeight.Bindings.Add(new Binding(nameof(BottomPad)) { Source = this });
            SetBinding(HeightProperty, bindingHeight);

            // LayoutTransform 바인딩
            var scaleTransform = new ScaleTransform();
            var bindingScale = new MultiBinding { Converter = new ScaleCalculator() };
            bindingScale.Bindings.Add(new Binding(nameof(Width)) { Source = this });
            bindingScale.Bindings.Add(new Binding(nameof(Height)) { Source = this });
            bindingScale.Bindings.Add(new Binding(nameof(TargetSize)) { Source = this });
            BindingOperations.SetBinding(scaleTransform, ScaleTransform.ScaleXProperty, bindingScale);
            BindingOperations.SetBinding(scaleTransform, ScaleTransform.ScaleYProperty, bindingScale);
            LayoutTransform = scaleTransform;

            var innerBorderOriginal = new Border
            { BorderThickness = new Thickness(1), BorderBrush = SystemColors.ControlDarkBrush };
            innerBorderOriginal.SetBinding(Canvas.LeftProperty, new Binding(nameof(LeftPad)) { Source = this });
            innerBorderOriginal.SetBinding(Canvas.TopProperty, new Binding(nameof(TopPad)) { Source = this });
            Children.Add(innerBorderOriginal);

            var originalImage = new Image { Opacity = 0.5, Margin = new Thickness(-1) };
            originalImage.SetBinding(Image.SourceProperty, new Binding(nameof(OriginalSource)) { Source = this });
            innerBorderOriginal.Child = originalImage;

            innerBorderAlternative = new Border
            { BorderThickness = new Thickness(1), BorderBrush = SystemColors.ControlDarkBrush, Visibility = Visibility.Hidden };
            Children.Add(innerBorderAlternative);
            innerBorderAlternative.SetValue(RightProperty, 0.0);
            innerBorderAlternative.SetValue(TopProperty, 0.0);

            var alternativeImage = new Image { Opacity = 0.5, Margin = new Thickness(-1) };
            alternativeImage.SetBinding(Image.SourceProperty, new Binding(nameof(AlternativeSource)) { Source = this });
            innerBorderAlternative.Child = alternativeImage;
        }

        protected override void OnPreviewDrop(DragEventArgs e)
        {
            base.OnPreviewDrop(e);

            if(e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if(e.Data.GetData(DataFormats.FileDrop) is string[] filePaths)
                {
                    BitmapImage importedImage = null;

                    try { importedImage = BitmapImageFromFile(filePaths[0]); }
                    catch(Exception ex)
                    {
                        MessageBox.Show(ex.Message + "\n" + filePaths[0], "Image load error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    int targetWidth = OriginalSource.PixelWidth + LeftPad + RightPad;
                    int targetHeight = OriginalSource.PixelHeight + TopPad + BottomPad;
                    if(importedImage.PixelWidth != targetWidth || importedImage.PixelHeight != targetHeight)
                        MessageBox.Show("Target dimension : " + targetWidth + "×" + targetHeight + "\n" +
                            "Imported image dimension : " + importedImage.PixelWidth + "×" + importedImage.PixelHeight + "\n",
                            "Image dimension mismatched", MessageBoxButton.OK, MessageBoxImage.Error);
                    else SetValue(AlternativeSourceProperty, importedImage);
                }
            }
        }

        private static BitmapImage BitmapImageFromFile(string filePath)
        {
            // 원본 Bitmap 로드 및 DPI 변경
            var originalBitmap = new System.Drawing.Bitmap(filePath);
            originalBitmap.SetResolution(96, 96);

            // 메모리 스트림 잡고 PNG 저장
            using MemoryStream stream = new MemoryStream();
            originalBitmap.Save(stream, ImageFormat.Png);
            originalBitmap.Dispose();

            // BitmapImage로 변환
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();

            return image;
        }


        class SumCalculator : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                double result = 0;
                foreach(object curVal in values)
                {
                    if(curVal is string valStr && double.TryParse(valStr, out double d)) result += d;
                    else if(curVal.AsDouble() is double curDouble) result += curDouble;
                }
                return result;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
                => throw new NotImplementedException();
        }


        class ScaleCalculator : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if(!(values.Length > 2 && values[0] is double w &&
                    values[1] is double h && values[2] is Size s)) return 1.0;
                double horizontalScale = s.Width / w;
                double verticalScale = s.Height / h;
                return Math.Min(horizontalScale, verticalScale);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
                => throw new NotImplementedException();
        }
    }
}
