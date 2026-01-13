using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Core;
using IndoorMapTools.Model;
using IndoorMapTools.Services.Domain;
using MapView.System.Windows.Controls;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static IndoorMapTools.View.FGAView.FGAVisuals.FGAEdge;

namespace IndoorMapTools.Helper
{
    internal abstract class ConverterBase : MarkupExtension
    { public override object ProvideValue(IServiceProvider serviceProvider) => this; }

    internal abstract class OneWayConverter : ConverterBase, IValueConverter
    {
        public abstract object Convert(object value);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => Convert(value);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    internal abstract class OneWayConverter<T> : ConverterBase, IValueConverter
    {
        public abstract object Convert(T value);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is T instance) ? Convert(instance) : default;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    internal abstract class OneWayConverterParam : ConverterBase, IValueConverter
    {
        public abstract object Convert(object value, object parameter);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => Convert(value, parameter);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    internal abstract class OneWayMultiConverter : ConverterBase, IMultiValueConverter
    {
        public abstract object Convert(object[] values);

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            => Convert(values);

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    internal abstract class OneWayMultiConverterParam : ConverterBase, IMultiValueConverter
    {
        public abstract object Convert(object[] values, object parameter);

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            => Convert(values, parameter);

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    class IsNull : ConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value == null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool vbool && vbool) ? null : Binding.DoNothing;
    }

    class IsNotNull : ConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value != null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool vbool && !vbool) ? null : Binding.DoNothing;
    }

    class Compare : OneWayMultiConverterParam
    {
        public override object Convert(object[] values, object parameter)
        {
            if(!(values[0].AsDouble() is double a && values[1].AsDouble() is double b && parameter is string op)) return false;
            return op switch
            {
                "eq" => (a - b) < 0.000001,
                "ne" => (a - b) > 0.000001,
                "gt" => a > b,
                "lt" => a < b,
                "ge" => a > b || ((a - b) < 0.000001),
                "le" => a < b || ((a - b) < 0.000001),
                _ => false
            };
        }
    }

    class Multiplier : ConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value.AsDouble() is double dVal && parameter.AsDouble() is double dParam) ? dVal * dParam : 0.0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value.AsDouble() is double dVal && parameter.AsDouble() is double dParam) ? dVal / dParam : 0.0;
    }

    class ForceInt : ConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value.AsDouble() is double dVal) ? (int)dVal : 0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value.AsDouble() is double dVal) ? (int)dVal : 0;
    }

    class PointDoubleDivider : OneWayMultiConverter
    {
        public override object Convert(object[] values)
        {
            if(values[0] is Point p && values[1].AsDouble() is double d)
                return new Point(p.X / d, p.Y / d);
            return default;
        }
    }

    class XYToPointConverter : ConverterBase, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            => ((values[0].AsDouble() is double xVal) && values[1].AsDouble() is double yVal) ?
                new Point(xVal, yVal) : default;

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => (value is Point vPoint) ? new object[] { vPoint.X, vPoint.Y } : default;
    }

    class PointArrayToPointCollection : ConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is Point[] points) ? new PointCollection(points) : default;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is PointCollection points) ? points.ToArray() : default;
    }

    class GetType : ConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.GetType() ?? Binding.DoNothing;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.GetType() ?? Binding.DoNothing;
    }

    /// <summary>
    /// value가 ConverterParameter로 명시한 타입일 경우에만 그대로 통과
    /// 해당 타입이 아닐 경우 null로 변환
    /// </summary>
    class TypeFilter : ConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value != null && (value.GetType() == (parameter as Type))) ? value : null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value != null && (value.GetType() == (parameter as Type))) ? value : null;
    }

    /// <summary>
    /// value가 ConverterParameter로 명시한 타입일 경우에만 그대로 통과
    /// 해당 타입이 아닐 경우 Binding.DoNothing 으로 변환 
    /// </summary>
    class ConservativeTypeFilter : ConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value != null && parameter != null && (value.GetType() == (parameter as Type))) ? value : Binding.DoNothing;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value != null && parameter != null && (value.GetType() == (parameter as Type))) ? value : Binding.DoNothing;
    }

    class IntEqualsTo : ConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => System.Convert.ToInt32(value) == System.Convert.ToInt32(parameter);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => ((bool)value) ? System.Convert.ToInt32(parameter) : 0;
    }

    class ObjectEquals : ConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.Equals(parameter);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.Equals(true) == true ? parameter : Binding.DoNothing;
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

    class NullityToVisibility : OneWayConverter
    { public override object Convert(object value) => (value != null) ? Visibility.Visible : Visibility.Hidden; }

    class NonNullityToVisibility : OneWayConverter
    { public override object Convert(object value) => (value != null) ? Visibility.Hidden : Visibility.Visible; }

    class GetIndex : OneWayMultiConverter
    {
        public override object Convert(object[] values)
            => (values[1] is System.Collections.IList list) ? list.IndexOf(values[0]) : -1;
    }

    class IndexMapper : OneWayMultiConverter
    {
        public override object Convert(object[] values)
            => (values[0] is int index && values[1] is int[] map && index >= 0 && map.Length > index) ? map[index] : 0;
    }

    class DictKeytoValue : OneWayMultiConverter
    {
        public override object Convert(object[] values)
            => (values[0] is IDictionary dict && values[1] != null && dict.Contains(values[1])) ? dict[values[1]] : default;
    }

    class ThicknessConverter : OneWayMultiConverter
    {
        public override object Convert(object[] values)
            => (values[0].AsDouble() is double left && values[1].AsDouble() is double top &&
                values[2].AsDouble() is double right && values[3].AsDouble() is double bottom) ?
                new Thickness(left, top, right, bottom) : default;
    }

    class LocationToPointConverter : OneWayConverter<Location>
    { public override object Convert(Location loc) => new Point(loc.Longitude, loc.Latitude); }

    class PointToLocationConverter : ConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is Point loc) ? new Location(loc.Y, loc.X) : default;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is Location loc) ? new Point(loc.Longitude, loc.Latitude) : default;
    }

    class LandmarkCounter : OneWayConverterParam
    {
        public override object Convert(object value, object parameter)
            => (value as IEnumerable<LandmarkGroup>).Count(group => group.Type == (LandmarkType)parameter);
    }

    class LandmarkExtractor : OneWayConverter
    {
        public override object Convert(object value)
        {
            if(value is not Building building) return null;
            var result = new List<Landmark>();
            foreach(LandmarkGroup curGroup in building.LandmarkGroups)
                foreach(Landmark curLM in curGroup.Landmarks)
                    result.Add(curLM);
            return result;
        }
    }

    //class LandmarkEdgeExtractor : OneWayMultiConverter
    //{
    //    public override object Convert(object[] values)
    //    {
    //        if(!(values[0] is Building building && 
    //            values[1] is Dictionary<Landmark, GraphNode> landmarktoNode)) return null;
            
    //        List<object[]> result = new List<object[]>();
    //        var floorList = building.Floors;

    //        foreach(LandmarkGroup group in building.LandmarkGroups)
    //        {
    //            if(group.Landmarks.Count < 2) continue;

    //            var tempLandmarks = group.Landmarks.OrderBy(lm => floorList.IndexOf(lm.ParentFloor)).ToList();

    //            for(int i = 1; i < tempLandmarks.Count; i++)
    //            {
    //                var startNode = landmarktoNode[tempLandmarks[i - 1]];
    //                var endNode = landmarktoNode[tempLandmarks[i]];
    //                result.Add(new object[] { startNode.Floor, startNode.Group, startNode.Area,
    //                    endNode.Floor, endNode.Group, endNode.Area});
    //            }
    //        }

    //        return result;
    //    }
    //}

    //class RecursiveEdgeExtractor : OneWayConverter
    //{
    //    public override object Convert(object value)
    //    {
    //        if(!(value is GraphNode rootNode)) return default;

    //        List<object[]> result = new List<object[]>();
    //        var nodeScanQueue = new Queue<GraphNode>();
    //        var checkedNodes = new HashSet<GraphNode>(); // 체크된 노드들

    //        nodeScanQueue.Enqueue(rootNode); // BFS 시작
    //        while(nodeScanQueue.Count > 0)
    //        {
    //            GraphNode curNode = nodeScanQueue.Dequeue(); // 큐에서 꺼내기
    //            if(checkedNodes.Contains(curNode)) continue;
    //            checkedNodes.Add(curNode); // 체크된 노드에 추가

    //            foreach(GraphNode nextNode in curNode.Children)
    //            {
    //                result.Add(new object[] { curNode.Floor, curNode.Group, curNode.Area,
    //                    nextNode.Floor, nextNode.Group, nextNode.Area, 
    //                    nextNode.Children.Contains(curNode) ? EdgeHeader.Circle : EdgeHeader.Arrow });
    //                nodeScanQueue.Enqueue(nextNode); // 큐에 추가
    //            }
    //        }

    //        return result;
    //    }
    //}


    class GetGroupIds : OneWayConverter
    {
        public override object Convert(object value)
        {
            if(value is not IList<Landmark> nodes) return Array.Empty<int>();
            if(nodes.Count == 0) Console.WriteLine("GetGroupIds: Empty landmark list received.");
            return nodes.Select(lm => lm.ParentGroup.ParentBuilding.LandmarkGroups.IndexOf(lm.ParentGroup)).ToArray();
        }
    }


    //class GetAreaPresentation : OneWayConverter
    //{
    //    public override object Convert(object value)
    //    {
    //        if(!(value is List<GraphNode> nodes && nodes.Count > 0)) return default;

    //        List<object[]> result = new List<object[]>();
    //        var landmarkPresentCell = new HashSet<int>();

    //        landmarkPresentCell.Clear();
    //        int minGroup = nodes[0].Group;
    //        int maxGroup = nodes[0].Group;

    //        foreach(var node in nodes)
    //        {
    //            landmarkPresentCell.Add(node.Group);
    //            if(node.Group < minGroup) minGroup = node.Group;
    //            if(node.Group > maxGroup) maxGroup = node.Group;
    //        }

    //        for(int groupIndex = minGroup; groupIndex <= maxGroup; groupIndex++)
    //        {
    //            bool left = (groupIndex > minGroup);
    //            bool center = landmarkPresentCell.Contains(groupIndex);
    //            bool right = (groupIndex < maxGroup);
                    
    //            result.Add(new object[] { nodes[0].Floor, groupIndex, nodes[0].Area, left, center, right });
    //        }

    //        return result;
    //    }
    //}

    internal class InverseTransformCache
    {
        private static BitmapSource originalImage;
        private static double rotation;
        private static double cellSize;
        private static double originalActualHeight;

        private static TransformGroup transform = new TransformGroup();

        private static void CheckCache(
            BitmapSource originalImage,
            double rotation,
            double cellSize,
            double originalActualHeight)
        {
            if(InverseTransformCache.originalImage == originalImage &&
                Math.Abs(InverseTransformCache.rotation - rotation) < 0.00001 &&
                Math.Abs(InverseTransformCache.cellSize - cellSize) < 0.00001 &&
                Math.Abs(InverseTransformCache.originalActualHeight - originalActualHeight) < 0.00001)
                return;

            InverseTransformCache.originalImage = originalImage;
            InverseTransformCache.rotation = rotation;
            InverseTransformCache.cellSize = cellSize;
            InverseTransformCache.originalActualHeight = originalActualHeight;

            double w = originalImage.PixelWidth;
            double h = originalImage.PixelHeight;

            var matrix = new Matrix();
            matrix.Rotate(rotation);

            Point pivot = new Point(0, h);
            Point rotatedPivot = matrix.Transform(pivot);

            Point[] corners = new[]
            {
                matrix.Transform(new Point(0, 0)),
                matrix.Transform(new Point(w, 0)),
                matrix.Transform(new Point(w, h)),
                matrix.Transform(new Point(0, h)),
            };

            double minX = corners.Min(p => p.X);
            double maxX = corners.Max(p => p.X);
            double minY = corners.Min(p => p.Y);
            double maxY = corners.Max(p => p.Y);

            double rotatedWidth = maxX - minX;
            double rotatedHeight = maxY - minY;

            // 회전 후 중심의 위치 (크롭 전 기준)
            double cx = rotatedPivot.X - minX;
            double cy = rotatedPivot.Y - minY;

            // 크롭된 양 계산
            double croppedRight = rotatedWidth - Math.Floor(rotatedWidth / cellSize) * cellSize;
            double croppedTop = rotatedHeight - Math.Floor(rotatedHeight / cellSize) * cellSize;

            // 중심을 보정한 위치로 이동
            double tx = -cx;
            double ty = -(cy - croppedTop); // 여기서 croppedTop을 보정해야 맞음

            double pixelToDip = originalActualHeight / h;

            var group = new TransformGroup();
            group.Children.Add(new RotateTransform(-rotation, 0, originalActualHeight));
            group.Children.Add(new TranslateTransform(tx * pixelToDip, ty * pixelToDip + originalActualHeight));
            transform = group;
        }


        public static TransformGroup GetintermediateDimension(BitmapSource originalImage,
            double rotation, double cellSize, double originalActualHeight)
        {
            CheckCache(originalImage, rotation, cellSize, originalActualHeight);
            return transform;
        }
    }

    class GetInverseTransform : OneWayMultiConverter
    {
        public override object Convert(object[] values)
        {
            if(!(values.Length == 5 && values[0] is BitmapSource originalImage && values[1] is double rotation && 
                values[1] is double ppm && values[1] is double ogmRes && values[4] is double originalActualHeight && originalActualHeight > 0.1))
                return Binding.DoNothing;
            //Console.WriteLine(values[0] + ", " + values[1] + ", " + values[2] + ", " + values[3] + ", " + values[4]);
            return InverseTransformCache.GetintermediateDimension(originalImage, rotation, 1.0 / ppm / ogmRes, originalActualHeight);
        }
    }

    class MouseToolPosition : OneWayConverter<ICommand>
    { public override object Convert(ICommand com) => new RelayCommand<MouseToolEventArgs>(e => com.Execute(e.Position)); }

    class MouseToolDragBox : OneWayConverter<ICommand>
    { public override object Convert(ICommand com) => new RelayCommand<MouseToolEventArgs>(e => com.Execute(e.DragBox)); }

    class PolygonCenter : ConverterBase, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is Point[] points) ? MathAlgorithms.CalculatePolygonCenter(points) : DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => new Point[] { (value is Point p) ? p : default };
    }

    class OutlineToCenterMeter : OneWayMultiConverter
    {
        public override object Convert(object[] values)
        {
            if(values.Length < 3 || values[0] is not Point[] outline || values[1] is not int mapImagePixelHeight 
                || values[2] is not double mapImagePPM) return DependencyProperty.UnsetValue;
            Point pixelCoord = MathAlgorithms.CalculatePolygonCenter(outline);
            return CoordCalculationService.PixelCoordToMeterCoord(pixelCoord, mapImagePixelHeight, mapImagePPM);
        }
    }

    class GetAppResource : OneWayConverterParam
    {
        public override object Convert(object value, object parameter)
            => (parameter is string postfix) ? Application.Current.Resources[value + postfix] : null;
    }
}
