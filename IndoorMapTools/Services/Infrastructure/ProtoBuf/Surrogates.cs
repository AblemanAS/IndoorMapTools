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

using IndoorMapTools.Core;
using ProtoBuf;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.Services.Infrastructure.ProtoBuf
{
    /// <summary>
    /// BitmapImage를 바이너리 형식으로 직렬화하기 위한 서로게이트 클래스
    /// 바이너리만 직렬화
    /// </summary>
    [ProtoContract]
    internal class MapImageSurrogate
    {
        [ProtoMember(1)] public byte[] Binary { get; set; }

        public static explicit operator MapImageSurrogate(BitmapImage source)
        {
            if(source == null) return null;

            MapImageSurrogate surrogate = new MapImageSurrogate();

            using MemoryStream stream = new MemoryStream();
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            encoder.Save(stream);
            surrogate.Binary = stream.ToArray();

            return surrogate;
        }

        public static explicit operator BitmapImage(MapImageSurrogate surrogate)
            => surrogate == null || surrogate.Binary == null ? null : ImageAlgorithms.BitmapImageFromBuffer(surrogate.Binary);
    }

    /// <summary>
    /// WriteableBitmap을 바이너리 형식으로 직렬화하기 위한 서로게이트 클래스
    /// Bitmap 바이너리 및 PixelWidth, PixelHeight로 직렬화
    /// </summary>
    [ProtoContract]
    internal class ReachableSurrogate // 96DPI, Indexed8
    {
        [ProtoMember(1)] public byte[] Binary { get; set; }
        [ProtoMember(2)] public int PixelWidth { get; set; }
        [ProtoMember(3)] public int PixelHeight { get; set; }

        public static explicit operator ReachableSurrogate(WriteableBitmap source)
        {
            if(source == null) return null;

            ReachableSurrogate surrogate = new ReachableSurrogate();

            source.Dispatcher.Invoke(() =>
            {
                surrogate.Binary = source.ToArray();
                surrogate.PixelWidth = source.PixelWidth;
                surrogate.PixelHeight = source.PixelHeight;
            });

            return surrogate;
        }

        public static explicit operator WriteableBitmap(ReachableSurrogate surrogate)
        {
            if(surrogate == null || surrogate.Binary == null) return null;

            WriteableBitmap reachable = ReachableAlgorithms.CreateReachable(surrogate.PixelWidth, surrogate.PixelHeight);
            reachable.Dispatcher.Invoke(() => reachable.FromArray(surrogate.Binary));
            return reachable;
        }
    }

    /// <summary>
    /// Point를 직렬화하기 위한 서로게이트 클래스
    /// X, Y를 각각 double로 직렬화
    /// </summary>
    [ProtoContract]
    internal class PointSurrogate
    {
        [ProtoMember(1)] public double X { get; set; }
        [ProtoMember(2)] public double Y { get; set; }

        public static explicit operator PointSurrogate(Point point) => new PointSurrogate { X = point.X, Y = point.Y };
        public static explicit operator Point(PointSurrogate surrogate) => new Point(surrogate.X, surrogate.Y);
    }
}
