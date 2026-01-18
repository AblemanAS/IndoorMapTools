using IndoorMapTools.Model;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace IndoorMapTools.MVVMExtensions.Markup
{
    [MarkupExtensionReturnType(typeof(object))]
    public sealed class DictGetValue : MarkupExtension
    {
        /// <summary>Key 모델 (기본값: 현재 DataContext)</summary>
        public BindingBase Key { get; set; } = default;

        /// <summary>PresentationStateMap 바인딩 (필수)</summary>
        public BindingBase Map { get; set; } = default!;

        /// <summary>바인딩 모드 (기본 OneWay)</summary>
        public BindingMode Mode { get; set; } = BindingMode.Default;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if(Key == null) throw new InvalidOperationException("Key binding must be provided.");
            if(Map == null) throw new InvalidOperationException("Map binding must be provided.");

            return new MultiBinding
            {
                Bindings = { Key, Map },
                Mode = Mode,
                Converter = new GetValue()
            }.ProvideValue(serviceProvider);
        }


        private sealed class GetValue : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if(values.Length < 2 || values[0] is not object key || values[1] is not object map) return null;
                var indexer = map.GetType().GetProperty("Item");
                if(indexer == null) return null;
                var param = indexer.GetIndexParameters();
                if(param.Length != 1) return null;
                if(!param[0].ParameterType.IsInstanceOfType(key)) return null;
                return indexer.GetValue(map, new[] { key });
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
                => throw new NotSupportedException();
        }
    }
}
