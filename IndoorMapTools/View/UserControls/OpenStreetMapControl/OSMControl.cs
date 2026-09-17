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

using Microsoft.Maps.MapControl.WPF;
using Microsoft.Maps.MapControl.WPF.Overlays;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Map = Microsoft.Maps.MapControl.WPF.Map;
using Microsoft.Maps.MapControl.WPF.Core;
using MapView.Controls;

namespace IndoorMapTools.OpenStreetMapControl
{
    public class OSMControl : Map
    {
        private const string MAP_TILE_SOURCE_URL = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
        private const string RESOURCE_FAILED_TILE_IMAGE = "FailedTile";
        private const int MAX_OSM_ZOOM_SUPPORTED = 19;

        [Bindable(true)]
        public new Point Center
        { get => (Point)GetValue(CenterProperty); set => SetValue(CenterProperty, value); }
        public static new readonly DependencyProperty CenterProperty =
            DependencyProperty.Register(nameof(Center), typeof(Point), typeof(OSMControl),
                new FrameworkPropertyMetadata(OnCenterChanged) { BindsTwoWayByDefault = true });

        private static void OnCenterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is OSMControl instance && e.NewValue is Point loc)
                ((MapCore)instance).Center = new Location(loc.Y, loc.X);
        }

        [Bindable(true)]
        public string AltTileSourceURL
        {
            get => (string)GetValue(AltTileSourceURLroperty); 
            set => SetValue(AltTileSourceURLroperty, value);
        }
        public static readonly DependencyProperty AltTileSourceURLroperty =
            DependencyProperty.Register(nameof(AltTileSourceURL), typeof(string), typeof(OSMControl),
                new FrameworkPropertyMetadata(OnAlternativeTileSourceURLChanged));

        private static void OnAlternativeTileSourceURLChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = (OSMControl)d;
            instance.tileLayer.Configure(instance.AltTileSourceURL ?? MAP_TILE_SOURCE_URL,
                instance.TileSourceHeaders, instance.TileZoomOffset, instance.TileMinZoom, instance.TileMaxZoom);
            instance.InvalidateVisual();
        }

        public IReadOnlyDictionary<string, string> TileSourceHeaders
        {
            get => (IReadOnlyDictionary<string, string>)GetValue(TileSourceHeadersProperty);
            set => SetValue(TileSourceHeadersProperty, value);
        }
        public static readonly DependencyProperty TileSourceHeadersProperty =
            DependencyProperty.Register(nameof(TileSourceHeaders), typeof(IReadOnlyDictionary<string, string>), typeof(OSMControl),
                new PropertyMetadata(null, OnAlternativeTileSourceURLChanged));

        public int TileZoomOffset
        {
            get => (int)GetValue(TileZoomOffsetProperty);
            set => SetValue(TileZoomOffsetProperty, value);
        }
        public static readonly DependencyProperty TileZoomOffsetProperty =
            DependencyProperty.Register(nameof(TileZoomOffset), typeof(int), typeof(OSMControl),
                new PropertyMetadata(0, OnAlternativeTileSourceURLChanged));

        public int TileMinZoom
        {
            get => (int)GetValue(TileMinZoomProperty);
            set => SetValue(TileMinZoomProperty, value);
        }
        public static readonly DependencyProperty TileMinZoomProperty =
            DependencyProperty.Register(nameof(TileMinZoom), typeof(int), typeof(OSMControl),
                new PropertyMetadata(0, OnAlternativeTileSourceURLChanged));

        public int TileMaxZoom
        {
            get => (int)GetValue(TileMaxZoomProperty);
            set => SetValue(TileMaxZoomProperty, value);
        }
        public static readonly DependencyProperty TileMaxZoomProperty =
            DependencyProperty.Register(nameof(TileMaxZoom), typeof(int), typeof(OSMControl),
                new PropertyMetadata(MAX_OSM_ZOOM_SUPPORTED, OnAlternativeTileSourceURLChanged));


        [Bindable(true)]
        public ObservableCollection<Point> BuildingOutline
        {
            get => (ObservableCollection<Point>)GetValue(BuildingOutlineProperty);
            set => SetValue(BuildingOutlineProperty, value);
        }
        public static readonly DependencyProperty BuildingOutlineProperty =
            DependencyProperty.Register(nameof(BuildingOutline), typeof(ObservableCollection<Point>), typeof(OSMControl),
                new PropertyMetadata(OnBuildingOutlineChanged));

        private static void OnBuildingOutlineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OSMControl instance = (OSMControl)d;
            if(e.OldValue is ObservableCollection<Point> oldINCC) oldINCC.CollectionChanged -= instance.OnCollectionChanged;
            if(e.NewValue is ObservableCollection<Point> newINCC)
            {
                newINCC.CollectionChanged += instance.OnCollectionChanged;
                instance.outlinePolygon.Locations.Clear();
                foreach(Point node in newINCC)
                    instance.outlinePolygon.Locations.Add(new Location(node.Y, node.X));
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach(Point newItem in e.NewItems)
                        outlinePolygon.Locations.Add(new Location(newItem.Y, newItem.X));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    outlinePolygon.Locations.RemoveAt(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    outlinePolygon.Locations[e.OldStartingIndex] = new Location(((Point)e.NewItems[0]).Y, ((Point)e.NewItems[0]).X);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    outlinePolygon.Locations.Clear();
                    foreach(Point node in BuildingOutline)
                        outlinePolygon.Locations.Add(new Location(node.Y, node.X));
                    break;
            }
        }

        [Bindable(true)]
        public MouseTool ActiveTool
        {
            get => (MouseTool)GetValue(ActiveToolProperty);
            set => SetValue(ActiveToolProperty, value);
        }
        public static readonly DependencyProperty ActiveToolProperty =
            DependencyProperty.Register(nameof(ActiveTool), typeof(MouseTool), typeof(OSMControl),
                new FrameworkPropertyMetadata(OnActiveToolChanged) { BindsTwoWayByDefault = true });

        private static void OnActiveToolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as OSMControl;
            if(e.NewValue is MouseTool newTool)
                instance.Cursor = newTool.DefaultCursor;
            else instance.Cursor = instance.UngrabCursor;
        }


        [Bindable(true)]
        public ICommand OnFileDropCommand
        {
            get => (ICommand)GetValue(OnFileDropCommandProperty);
            set => SetValue(OnFileDropCommandProperty, value);
        }
        public static readonly DependencyProperty OnFileDropCommandProperty =
            DependencyProperty.Register(nameof(OnFileDropCommand), typeof(ICommand), typeof(OSMControl));

        public Cursor GrabCursor { get; set; }
        public Cursor ungrabCursor;
        public Cursor UngrabCursor { get => ungrabCursor; set { ungrabCursor = value;  Cursor = ungrabCursor; } }

        private readonly OSMTileLayer tileLayer;
        private readonly MapPolygon outlinePolygon;


        public OSMControl()
        {
            IsTabStop = false;
            AllowDrop = true;
            SetValue(ModeProperty, new MercatorMode());
            SetValue(CredentialsProviderProperty, null);

            // 맵 레이어 초기화
            LayoutUpdated += RemoveOverlayTextBlock;
            tileLayer = new OSMTileLayer();
            Children.Add(tileLayer);

            // 폴리곤 레이어 초기화
            outlinePolygon = new MapPolygon();
            outlinePolygon.Locations = new LocationCollection();
            outlinePolygon.Fill = new SolidColorBrush(Colors.White) { Opacity = 0.25 };
            outlinePolygon.Stroke = new SolidColorBrush(Colors.Black);
            outlinePolygon.StrokeThickness = 1;
            outlinePolygon.StrokeLineJoin = PenLineJoin.Bevel;
            Children.Add(outlinePolygon);

            ViewChangeEnd += (sender, e) =>
            {
                Location changedViewLoc = ((MapCore)this).Center;
                SetCurrentValue(CenterProperty, new Point(changedViewLoc.Longitude, changedViewLoc.Latitude));
            };

            //IsVisibleChanged += (_, __) =>
            //{
            //    if(IsVisible && !Children.Contains(tileLayer)) Children.Insert(0, tileLayer);
            //    else Children.Remove(tileLayer);
            //};
        }

        private void RemoveOverlayTextBlock(object sender, EventArgs e)
        {
            if(VisualChildrenCount == 0) return;
            if(!((GetVisualChild(0) as Border).Child is Grid mapContainer)) return;
            var mapLayer = mapContainer.Children[1] as MapLayer;

            foreach(var item in mapLayer.Children)
            {
                if(item is LoadingErrorMessage errText)
                {
                    var mapForeground = (mapContainer.Children[2] as Grid).Children[0] as MapForeground;
                    var rtGrid = (VisualTreeHelper.GetChild(mapForeground, 0) as Grid).Children[0] as Grid;

                    // 에러 메시지 및 Logo Hide
                    errText.Visibility = Visibility.Hidden; // 지도 서비스 제공자 라이센스 에러 메시지
                    rtGrid.Children[0].Visibility = Visibility.Hidden; // Logo

                    // Copyright Text 변경
                    var spCopyrightParent = rtGrid.Children[1] as StackPanel;
                    spCopyrightParent.Children.RemoveAt(1);
                    spCopyrightParent.Children.Add(new TextBlock
                    {
                        Text = "© OpenStreetMap",
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Effect = new DropShadowEffect
                        {
                            Color = Colors.White,
                            BlurRadius = 2,
                            ShadowDepth = 2,
                            RenderingBias = RenderingBias.Performance
                        }
                    });

                    LayoutUpdated -= RemoveOverlayTextBlock;
                }
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if(Visibility != Visibility.Visible) return;

            switch(e.ChangedButton)
            {
                case MouseButton.XButton1: // Back button
                    ActiveTool?.OnMouseXButton1Down?.Execute(GenerateMouseToolEvent(e));
                    break;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if(Visibility != Visibility.Visible) return;
            
            if(ActiveTool == null)
            {
                if(GrabCursor != null) Cursor = GrabCursor;  // 기본 툴 : 커서 변경
                base.OnMouseLeftButtonDown(e);
            }
            else // 기타 툴 : 클릭 커서 있으면 커서 변경 후, MouseTool LB Down 핸들러 호출
            {
                if(ActiveTool.ClickedCursor != null) Cursor = ActiveTool.ClickedCursor;
                ActiveTool.OnMouseLeftDown?.Execute(GenerateMouseToolEvent(e));
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if(Visibility != Visibility.Visible) return;

            if(ActiveTool == null)
            {
                if(UngrabCursor != null) Cursor = UngrabCursor;  // 기본 툴 : 커서 변경
                base.OnMouseLeftButtonUp(e);
            }
            else // 기타 툴
            {
                if(ActiveTool.ClickedCursor != null) // 클릭 커서가 따로 있을 경우
                    Cursor = ActiveTool.DefaultCursor; // 커서 변경

                // MouseTool LB Up 이벤트 호출 및 드래그 종료
                ActiveTool.OnMouseLeftUp?.Execute(GenerateMouseToolEvent(e));
                ReleaseMouseCapture();
            }
        }


        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if(Visibility != Visibility.Visible) return;

            // 버튼 트리거 툴일 경우 우클릭으로 툴 해제
            if(ActiveTool != null && ActiveTool.ActivationKey == Key.None)
                SetValue(ActiveToolProperty, null);
        }


        protected override void OnMouseMove(MouseEventArgs e)
        {
            if(Visibility != Visibility.Visible) return;

            // MouseTool Move 핸들러 호출
            if(ActiveTool != null) ActiveTool.OnMouseMove?.Execute(GenerateMouseToolEvent(e));
            else base.OnMouseMove(e);
        }


        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if(OnFileDropCommand != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                //string ERR_MSG_FILE = "다음 파일 로드 중 오류가 발생했습니다\n";
                //string ERR_TITLE_FILE = "파일 로드 중 오류 발생";

                foreach(string filePath in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    try { OnFileDropCommand.Execute(filePath); }
                    catch { }//(Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
                }
            }
        }


        private MouseToolEventArgs GenerateMouseToolEvent(MouseEventArgs e) => new MouseToolEventArgs
        {
            Position = TryViewportPointToLocation(e.GetPosition(this), out Location movedLoc) ?
            new Point(movedLoc.Longitude, movedLoc.Latitude) : default
        };

        private class OSMTileLayer : MapTileLayer
        {
            private string tileSourceURL = MAP_TILE_SOURCE_URL;
            private IReadOnlyDictionary<string, string> headers;
            private int zoomOffset;
            private int minZoom;
            private int maxZoom = MAX_OSM_ZOOM_SUPPORTED;
            private bool needsCoordinates;
            private static readonly string osmSourceId = TileCacheManager.CreateSourceId(
                MAP_TILE_SOURCE_URL, null, 0, 0, MAX_OSM_ZOOM_SUPPORTED);
            private string sourceId = osmSourceId;

            public void Configure(string url, IReadOnlyDictionary<string, string> requestHeaders,
                int offset, int minimum, int maximum)
            {
                tileSourceURL = url;
                headers = requestHeaders;
                zoomOffset = offset;
                minZoom = minimum;
                maxZoom = maximum;
                needsCoordinates = url.Contains("{lon}") || url.Contains("{lat}");
                sourceId = TileCacheManager.CreateSourceId(url, requestHeaders, offset, minimum, maximum);
            }

            private static BitmapImage failedImage;

            private static HttpClient client;
            private static HttpClient Client
            {
                get
                {
                    if(client == null)
                    {
                        client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
                        string appVersion = (string)Application.Current.FindResource("AppVersion");
                        client.DefaultRequestHeaders.UserAgent.Clear();
                        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("IndoorMapTools", appVersion));
                        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(Windows NT 10.0; Win64; x64)"));
                        client.DefaultRequestHeaders.Accept.ParseAdd("image/avif,image/webp,image/apng,image/*,*/*;q=0.8");
                    }

                    return client;
                }
            }

            public OSMTileLayer() => TileSource = new TileSource { DirectImage = GetImage };

            private BitmapImage GetImage(long x, long y, int z)
            {
                var image = GetSourceImage(x, y, z, tileSourceURL, headers, zoomOffset,
                    minZoom, maxZoom, needsCoordinates, sourceId);
                if(image == null && sourceId != osmSourceId)
                    image = GetSourceImage(x, y, z, MAP_TILE_SOURCE_URL, null, 0,
                        0, MAX_OSM_ZOOM_SUPPORTED, false, osmSourceId);
                return image ?? (failedImage ??= (BitmapImage)Application.Current.FindResource(RESOURCE_FAILED_TILE_IMAGE));
            }

            private BitmapImage GetSourceImage(long x, long y, int z, string url,
                IReadOnlyDictionary<string, string> requestHeaders, int offset, int minimum,
                int maximum, bool coordinates, string cacheId)
            {
                if(z < 0 || z > 62 || (long)z + offset < minimum) return null;
                long difference = Math.Max(0L, (long)z + offset - maximum);
                if(difference > 8 || difference > z) return null;
                int zoomDiff = (int)difference;
                var sourceTile = LoadOrDownloadTile(x >> zoomDiff, y >> zoomDiff, z - zoomDiff,
                    url, requestHeaders, offset, coordinates, cacheId);
                if(sourceTile == null || zoomDiff == 0) return sourceTile;
                return ExtractAndScaleSubTile(sourceTile, x, y, zoomDiff);
            }

            private static Uri CreateTileAddress(string url, long x, long y, int z, int offset, bool coordinates)
            {
                var culture = CultureInfo.InvariantCulture;
                string address = url.Replace("{x}", x.ToString(culture))
                    .Replace("{y}", y.ToString(culture))
                    .Replace("{z}", ((long)z + offset).ToString(culture));
                if(coordinates)
                {
                    double n = Math.Pow(2.0, z);
                    double lon = (x + 0.5) / n * 360.0 - 180.0;
                    double lat = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * (y + 0.5) / n))) * 180.0 / Math.PI;
                    address = address.Replace("{lon}", lon.ToString("R", culture))
                        .Replace("{lat}", lat.ToString("R", culture));
                }
                return new Uri(address);
            }

            private BitmapImage LoadOrDownloadTile(long x, long y, int z, string url,
                IReadOnlyDictionary<string, string> requestHeaders, int offset, bool coordinates, string cacheId)
            {
                try
                {
                    if(TileCacheManager.Instance.TryLoadTile(cacheId, x, y, z, out var cached))
                        return cached;
                    using var request = new HttpRequestMessage(HttpMethod.Get, CreateTileAddress(url, x, y, z, offset, coordinates));
                    if(requestHeaders != null)
                        foreach(var header in requestHeaders)
                            request.Headers.Add(header.Key, header.Value);
                    using var response = Client.SendAsync(request).GetAwaiter().GetResult();
                    if(!response.IsSuccessStatusCode)
                    {
                        Trace.TraceWarning("Tile request failed: HTTP {0}.", (int)response.StatusCode);
                        return null;
                    }
                    byte[] data = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                    using var ms = new MemoryStream(data);
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.StreamSource = ms;
                    img.EndInit();
                    img.Freeze();
                    try { TileCacheManager.Instance.SaveTile(cacheId, x, y, z, data); }
                    catch(IOException) { Trace.TraceWarning("Tile cache write failed."); }
                    catch(UnauthorizedAccessException) { Trace.TraceWarning("Tile cache write was denied."); }
                    return img;
                }
                catch(Exception ex)
                { Trace.TraceWarning("Tile request failed ({0}).", ex.GetType().Name); }

                return null;
            }

            private BitmapImage ExtractAndScaleSubTile(BitmapImage parentTile, long x, long y, int zoomDiff)
            {
                int subTiles = 1 << zoomDiff;
                int tileSize = 256;
                int subSize = tileSize / subTiles;

                int subX = (int)(x & subTiles - 1);
                int subY = (int)(y & subTiles - 1);

                var rect = new Int32Rect(subX * subSize, subY * subSize, subSize, subSize);
                var cropped = new CroppedBitmap(parentTile, rect);

                var visual = new DrawingVisual();
                using(var ctx = visual.RenderOpen())
                {
                    ctx.DrawImage(cropped, new Rect(0, 0, tileSize, tileSize));
                }

                var bmp = new RenderTargetBitmap(tileSize, tileSize, 96, 96, PixelFormats.Pbgra32);
                bmp.Render(visual);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                using var ms = new MemoryStream();
                encoder.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);

                var result = new BitmapImage();
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = ms;
                result.EndInit();
                result.Freeze();

                return result;
            }
        }
    }
}
