using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace IndoorMapTools.MVVMExtensions.Markup
{
    [MarkupExtensionReturnType(typeof(object))]
    public sealed class ArrayGetValue : MarkupExtension
    {
        /// <summary>Key 모델 (기본값: 현재 DataContext)</summary>
        public BindingBase Index { get; set; } = default;

        /// <summary>PresentationStateMap 바인딩 (필수)</summary>
        public BindingBase Source { get; set; } = default!;

        /// <summary>바인딩 모드 (기본 OneWay)</summary>
        public BindingMode Mode { get; set; } = BindingMode.Default;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if(Index == null) throw new InvalidOperationException("Key binding must be provided.");
            if(Source == null) throw new InvalidOperationException("Map binding must be provided.");

            return new MultiBinding
            {
                Bindings = { Index, Source },
                Mode = Mode,
                Converter = new GetValue()
            }.ProvideValue(serviceProvider);
        }


        private sealed class GetValue : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if(values.Length < 2) return Binding.DoNothing;
                if(values[0] is not int index || values[1] is not Array source) return Binding.DoNothing;
                if(index >= source.Length) return Binding.DoNothing;
                return source.GetValue(index);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
                => throw new NotSupportedException();
        }
    }
}
