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
using System.Windows.Data;
using System.Windows.Markup;

namespace IndoorMapTools.MVVMExtensions.Markup
{
    /// <summary>
    /// Dictionary의 Map, Key에 대한 Binding을 지원하지 않는 WPF XAML 기본 사양을 보강하기 위하여, 
    /// Map, Key에 바인딩을 할당하여 Markup Expression의 형태로 사용할 수 있는 Dictionaty 인덱싱 멀티바인딩
    /// </summary>
    [MarkupExtensionReturnType(typeof(object))]
    public sealed class DictGetValue : MarkupExtension
    {
        /// <summary>Key</summary>
        public BindingBase Key { get; set; } = default;

        /// <summary>Dictionary Map</summary>
        public BindingBase Map { get; set; } = default!;

        /// <summary>바인딩 모드 (기본 OneWay)</summary>
        public BindingMode Mode { get; set; } = BindingMode.Default;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if(Key == null) throw new InvalidOperationException("Key binding must be provided.");
            if(Map == null) throw new InvalidOperationException("Map binding must be provided.");
            return new MultiBinding { Bindings = { Key, Map }, Mode = Mode,
                Converter = new GetValue() }.ProvideValue(serviceProvider);
        }


        private sealed class GetValue : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if(values.Length < 2 || values[0] is not object key || values[1] is not object map) return DependencyProperty.UnsetValue;
                var indexer = map.GetType().GetProperty("Item");
                if(indexer == null) return DependencyProperty.UnsetValue;
                var param = indexer.GetIndexParameters();
                if(param.Length != 1) return DependencyProperty.UnsetValue;
                if(!param[0].ParameterType.IsInstanceOfType(key)) return DependencyProperty.UnsetValue;
                return indexer.GetValue(map, new[] { key });
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
                => throw new NotSupportedException();
        }
    }
}
