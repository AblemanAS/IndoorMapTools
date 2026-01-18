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

using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Algorithm;
using IndoorMapTools.Model;
using MapView.System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

namespace IndoorMapTools.Helper
{
    internal abstract class MarkupConverterBase : MarkupExtension
    { public override object ProvideValue(IServiceProvider serviceProvider) => this; }


    internal abstract class OneWayConverter : MarkupConverterBase, IValueConverter
    {
        public abstract object Convert(object value);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => Convert(value);
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }


    internal abstract class OneWayConverter<T> : MarkupConverterBase, IValueConverter
    {
        public abstract object Convert(T value);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is T instance) ? Convert(instance) : default;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }


    internal abstract class OneWayConverterParam : MarkupConverterBase, IValueConverter
    {
        public abstract object Convert(object value, object parameter);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => Convert(value, parameter);
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }


    internal abstract class OneWayMultiConverter : MarkupConverterBase, IMultiValueConverter
    {
        public abstract object Convert(object[] values);
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            => Convert(values);
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }


    internal abstract class OneWayMultiConverterParam : MarkupConverterBase, IMultiValueConverter
    {
        public abstract object Convert(object[] values, object parameter);
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            => Convert(values, parameter);
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }


    class IsNull : MarkupConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value == null;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool vbool && vbool) ? null : Binding.DoNothing;
    }


    class IsNotNull : MarkupConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value != null;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool vbool && !vbool) ? null : Binding.DoNothing;
    }


    class Multiplier : MarkupConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (AsDouble(value) is double dVal && AsDouble(parameter) is double dParam) ? dVal * dParam : 0.0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (AsDouble(value) is double dVal && AsDouble(parameter) is double dParam) ? dVal / dParam : 0.0;

        private static object AsDouble(object o)
        {
            try { return (double)System.Convert.ChangeType(o, TypeCode.Double); }
            catch { return (o is string str && double.TryParse(str, out double d)) ? d : null; }
        }
    }


    class GetType : MarkupConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.GetType() ?? Binding.DoNothing;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.GetType() ?? Binding.DoNothing;
    }


    class ObjectEqualsMulti : OneWayMultiConverter
    { public override object Convert(object[] values) => values != null && values.Length == 2 && values[0].Equals(values[1]); }


    class BoolToVisibility : OneWayConverter
    {
        public override object Convert(object value)
            => (value is bool isVIsible && isVIsible) ? Visibility.Visible : Visibility.Hidden;
    }


    class BoolToInvisibility : OneWayConverter
    {
        public override object Convert(object value)
            => (value is bool isInvisible && isInvisible) ? Visibility.Hidden : Visibility.Visible;
    }


    class NonNullToVisibility : OneWayConverter
    { public override object Convert(object value) => (value != null) ? Visibility.Visible : Visibility.Hidden; }


    class NullToVisibility : OneWayConverter
    { public override object Convert(object value) => (value == null) ? Visibility.Visible : Visibility.Hidden; }


    class LandmarkCounter : OneWayConverterParam
    {
        public override object Convert(object value, object parameter)
            => (value as IEnumerable<LandmarkGroup>).Count(group => group.Type == (LandmarkType)parameter);
    }


    class GetGroupIds : OneWayConverter
    {
        public override object Convert(object value)
        {
            if(value is not IList<Landmark> nodes) return Array.Empty<int>();
            // TODO : 고립 Area로서 추후 처리
            if(nodes.Count == 0) Console.WriteLine("GetGroupIds: Empty landmark list received."); 
            return nodes.Select(lm => lm.ParentGroup.ParentBuilding.LandmarkGroups.IndexOf(lm.ParentGroup)).ToArray();
        }
    }


    class GetGroupIdsReorder : OneWayMultiConverter
    {
        public override object Convert(object[] values)
        {
            if(values.Length != 2 || values[0] is not IList<Landmark> nodes ||
                values[1] is not int[] reorderMap) return Array.Empty<int>();
            // TODO : 고립 Area로서 추후 처리
            if(nodes.Count == 0) Console.WriteLine("GetGroupIds: Empty landmark list received.");
            return nodes.Select(lm => reorderMap[lm.ParentGroup.ParentBuilding.LandmarkGroups.IndexOf(lm.ParentGroup)]).ToArray();
        }
    }


    class MouseToolPosition : OneWayConverter<ICommand>
    { public override object Convert(ICommand com) => new RelayCommand<MouseToolEventArgs>(e => com.Execute(e.Position)); }


    class MouseToolDragBox : OneWayConverter<ICommand>
    { public override object Convert(ICommand com) => new RelayCommand<MouseToolEventArgs>(e => com.Execute(e.DragBox)); }


    class PolygonCenter : MarkupConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is Point[] points) ? CoordTransformAlgorithms.CalculatePolygonCenter(points) : DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => new Point[] { (value is Point p) ? p : default };
    }


    class OutlineToCenterMeter : OneWayMultiConverter
    {
        public override object Convert(object[] values)
        {
            if(values.Length < 3 || values[0] is not Point[] outline || values[1] is not int mapImagePixelHeight 
                || values[2] is not double mapImagePPM) return DependencyProperty.UnsetValue;
            Point pixelCoord = CoordTransformAlgorithms.CalculatePolygonCenter(outline);
            return CoordTransformAlgorithms.PixelCoordToMeterCoord(pixelCoord, mapImagePixelHeight, mapImagePPM);
        }
    }


    class GetAppResource : OneWayConverterParam
    {
        public override object Convert(object value, object parameter)
            => (value.ToString() is string name) ? Application.Current.Resources[name + ((parameter as string) ?? null)] : null;
    }

    class GetAppResourceOnTrue : OneWayConverterParam
    {
        public override object Convert(object value, object parameter)
            => ((value is bool tf) && tf && (parameter.ToString() is string name)) ? Application.Current.Resources[name] : null;
    }
}
