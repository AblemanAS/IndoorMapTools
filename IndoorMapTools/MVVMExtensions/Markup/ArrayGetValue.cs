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
    /// Array의 Source, Index에 대한 Binding을 지원하지 않는 WPF XAML 기본 사양을 보강하기 위하여, 
    /// Source, Index에 바인딩을 할당하여 Markup Expression의 형태로 사용할 수 있는 배열 인덱싱 멀티바인딩
    /// </summary>
    [MarkupExtensionReturnType(typeof(object))]
    public sealed class ArrayGetValue : MarkupExtension
    {
        /// <summary>Index</summary>
        public BindingBase Index { get; set; } = default;

        /// <summary>Array Source</summary>
        public BindingBase Source { get; set; } = default!;

        /// <summary>바인딩 모드 (기본 OneWay)</summary>
        public BindingMode Mode { get; set; } = BindingMode.Default;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if(Index == null) throw new InvalidOperationException("Index binding must be provided.");
            if(Source == null) throw new InvalidOperationException("Source binding must be provided.");
            return new MultiBinding { Bindings = { Index, Source }, Mode = Mode, 
                Converter = new GetValue() }.ProvideValue(serviceProvider);
        }


        private sealed class GetValue : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if(values.Length < 2) return DependencyProperty.UnsetValue;
                if(values[0] is not int index || values[1] is not Array source) return DependencyProperty.UnsetValue;
                if(index >= source.Length) return DependencyProperty.UnsetValue;
                return source.GetValue(index);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
                => throw new NotSupportedException();
        }
    }
}
