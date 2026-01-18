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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.View.UserControls
{
    /// <summary>
    /// 픽셀값에 따라 히트 테스트를 수행하는 이미지 컨트롤.
    /// Indexed1bpp BitmapImage만 지원.
    /// </summary>
    class Indexed1HitTestImage : Image
    {
        private byte[] pixelDataCache;
        private int stride;

        /// <summary>
        /// Source가 변경될 경우 픽셀 데이터를 캐시.
        /// 지원 포맷이 아닐 경우 null로 초기화.
        /// </summary>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if(e.Property != SourceProperty) return;
            
            if(!(Source is BitmapSource source && source.Format == PixelFormats.Indexed1))
            {
                pixelDataCache = null;
                return;
            }

            // Indexed1의 경우, 1픽셀이 1비트이므로 한 행의 바이트 수는 (PixelWidth + 7) / 8
            stride = (source.PixelWidth + 7) / 8;
            pixelDataCache = new byte[stride * source.PixelHeight];
            source.CopyPixels(pixelDataCache, stride, 0);
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            if(Source is BitmapSource source && pixelDataCache != null)
            {
                var x = (int)(hitTestParameters.HitPoint.X / ActualWidth * source.PixelWidth);
                var y = (int)(hitTestParameters.HitPoint.Y / ActualHeight * source.PixelHeight);

                // 픽셀값이 0일 경우 null 반환
                if(((pixelDataCache[y * stride + (x / 8)] >> (7 - (x % 8))) & 1) == 0) return null;
            }
            
            return base.HitTestCore(hitTestParameters);
        }
    }
}
