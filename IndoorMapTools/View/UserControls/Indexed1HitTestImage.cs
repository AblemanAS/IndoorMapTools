﻿using System;
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
        /// <param name="e"></param>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if(e.Property == SourceProperty)
            {
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
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            if(Source is BitmapImage source && pixelDataCache != null)
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
