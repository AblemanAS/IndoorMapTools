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
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Markup;

namespace IndoorMapTools.MVVMExtensions.ComponentModel
{
    /// <summary>
    /// Model마다 일일이 ViewModel을 생성하면 시스템 복잡도를 유발하는 경우를 회피하기 위하여 
    /// 모델을 래핑하지 않고 별도의 런타임 캐시를 통해 프레젠테이션 상태를 부착하는 컴포넌트 모델.  
    /// 스스로가 <see cref="System.ComponentModel.INotifyPropertyChanged"/> 로서, 
    /// 생성되는 TState들이 <see cref="System.ComponentModel.INotifyPropertyChanged"/> 일 경우 
    /// 해당 객체들에서의 PropertyChangedEvent들을 중계함. 이 때 이 이벤트의 sender는 TState가 아닌 TModel로 발신됨. 
    /// <para>XAML에서는 <c>stateMap[model]</c> 과 같은 인덱서 기반 접근 가능</para>
    /// <para>TState는 변경 알림을 위해 <see cref="System.ComponentModel.INotifyPropertyChanged"/> 구현을 강력히 권장함</para>
    /// </summary>
    /// <typeparam name="TModel">프레젠테이션 상태가 부착되는 기준 모델 타입</typeparam>
    /// <typeparam name="TState">모델 별 프레젠테이션 상태 객체 타입</typeparam>
    public sealed class PresentationStateMap<TModel, TState> : INotifyPropertyChanged where TModel : class where TState : class
    {
        private readonly ConditionalWeakTable<TModel, TState> table = new();
        private readonly Func<TModel, TState> factory;

        public event PropertyChangedEventHandler PropertyChanged;

        public PresentationStateMap(Func<TState> factory)
            : this(_ => (factory ?? throw new ArgumentNullException(nameof(factory)))()) { }

        public PresentationStateMap(Func<TModel, TState> factory)
            => this.factory = factory ?? throw new ArgumentNullException(nameof(factory));


        public TState Get(TModel model)
        {
            if(model == null) return null;

            // 이미 존재하면 반환하고 그대로 종료
            if(table.TryGetValue(model, out var state)) return state;

            // 그렇지 않을 경우 생성 후 추가 및 INPC에 한해 핸들러 붙임
            state = factory(model);
            table.Add(model, state);
            if(state is INotifyPropertyChanged inpc)
                inpc.PropertyChanged += (_, e) =>
                    PropertyChanged?.Invoke(model, new PropertyChangedEventArgs(e.PropertyName));

            return state;
        }


        public TState this[TModel key] => Get(key);
    }


    [MarkupExtensionReturnType(typeof(object))]
    public sealed class StateBindingExtension : MarkupExtension
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

            return new MultiBinding { Bindings = { Key, Map }, Mode = Mode, 
                Converter = new GetPresentationState() }.ProvideValue(serviceProvider);
        }


        private sealed class GetPresentationState : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if(values.Length < 2) return Binding.DoNothing;
                var key = values[0]; var map = values[1];
                if(key == null || map == null) return Binding.DoNothing;

                // PresentationStateMap<TModel, TState>의 인덱서(Item) 호출
                var indexer = map.GetType().GetProperty("Item");
                if(indexer == null) return Binding.DoNothing;
                var p = indexer.GetIndexParameters();
                if(p.Length != 1) return Binding.DoNothing;
                if(key == null || !p[0].ParameterType.IsInstanceOfType(key))
                    return Binding.DoNothing;
                return indexer.GetValue(map, new[] { key });
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
                => throw new NotSupportedException();
        }
    }
}
