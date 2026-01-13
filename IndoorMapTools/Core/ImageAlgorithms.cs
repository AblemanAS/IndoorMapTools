using IndoorMapTools.Model;
using System;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using Bitmap = System.Drawing.Bitmap;
using Drawing2D = System.Drawing.Drawing2D;
using Graphics = System.Drawing.Graphics;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace IndoorMapTools.Core
{
    public static class ImageAlgorithms
    {
        private class AllocationTerminator
        {
            private readonly MemoryStream stream;
            public AllocationTerminator(MemoryStream stream) => this.stream = stream;
            ~AllocationTerminator() => stream.Dispose();
        }

        public static BitmapImage BitmapImageFromBuffer(byte[] buffer)
        {
            using var stream = new MemoryStream(buffer);
            return BitmapImageFromMemoryStream(stream);
        }

        public static BitmapImage BitmapImageFromFile(string filePath)
        {
            // 원본 Bitmap 로드 및 DPI 변경
            using Bitmap originalBitmap = new Bitmap(filePath);
            originalBitmap.SetResolution(96, 96);

            // BitmapImage로 변환
            BitmapImage result = BitmapImageFromBitmap(originalBitmap);
            //Console.WriteLine("DPI : " + result.DpiX + ", " + result.DpiY);
            //Console.WriteLine("Format : " + result.Format);

            return result;
        }

        public static Bitmap ToBitmap(this BitmapSource source)
        {
            using var stream = new MemoryStream();
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(source));
            enc.Save(stream);
            var bitmap = new Bitmap(stream);
            bitmap.SetResolution(96, 96);

            return bitmap;
        }

        public static BitmapImage BitmapImageFromBitmap(Bitmap bitmap)
        {
            // 메모리 스트림 잡고 png 압축
            using MemoryStream stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return BitmapImageFromMemoryStream(stream);
        }


        public static BitmapImage BitmapImageFromMemoryStream(MemoryStream stream)
        {            
            // BitmapImage 생성
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }


        public static void DrawPolygonsOnBitmap(Bitmap bitmap, Point[][] polygons)
        {
            if(polygons == null) return;

            using Graphics g = Graphics.FromImage(bitmap);

            // System.Drawing.PointF array로 변환
            foreach(var polygon in polygons)
            {
                var polygonF = new System.Drawing.PointF[polygon.Length];
                for(int i = 0; i < polygon.Length; i++)
                {
                    polygonF[i].X = (float)polygon[i].X;
                    polygonF[i].Y = (float)polygon[i].Y;
                }
                g.FillPolygon(System.Drawing.Brushes.White, polygonF);
            }
        }

        public static Bitmap GetRotatedBitmap(Bitmap bitmap, double rotation, bool accurate = false)
        {
            double angle = rotation * Math.PI / 180.0;
            float newWidth = (float)(Math.Abs(bitmap.Width * Math.Cos(angle)) + Math.Abs(bitmap.Height * Math.Sin(angle)));
            float newHeight = (float)(Math.Abs(bitmap.Width * Math.Sin(angle)) + Math.Abs(bitmap.Height * Math.Cos(angle)));
            var rotatedImage = new Bitmap((int)Math.Ceiling(newWidth), (int)Math.Ceiling(newHeight), PixelFormat.Format32bppArgb);
            rotatedImage.SetResolution(96, 96);
            rotatedImage.MakeTransparent();

            using Graphics g = Graphics.FromImage(rotatedImage);

            if(accurate)
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
            }

            var matrix = new Drawing2D.Matrix();
            matrix.Translate((newWidth - bitmap.Width) / 2, (newHeight - bitmap.Height) / 2, Drawing2D.MatrixOrder.Append);
            matrix.RotateAt((float)rotation, new System.Drawing.PointF(newWidth / 2, newHeight / 2), Drawing2D.MatrixOrder.Append);
            g.Transform = matrix;
            g.InterpolationMode = Drawing2D.InterpolationMode.Bilinear;
            g.PixelOffsetMode = Drawing2D.PixelOffsetMode.None;
            g.Clear(System.Drawing.Color.Transparent);
            g.DrawImage(bitmap, new System.Drawing.PointF(0, 0));

            return rotatedImage;
        }


        public static BitmapImage GetPadCropImage(BitmapSource original, int leftPad, int topPad, int rightPad, int bottomPad)
        {
            int newWidth = original.PixelWidth + leftPad + rightPad;
            int newHeight = original.PixelHeight + topPad + bottomPad;

            var bufferImage = new WriteableBitmap(newWidth, newHeight, 96, 96, original.Format, original.Palette);
            CopyBitmapImageWithPads(bufferImage, original, leftPad, topPad, rightPad, bottomPad);

            // 새 스트림을 잡아 png로 인코딩
            using var stream = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bufferImage));
            encoder.Save(stream);

            // BitmapImage 핸들을 따서 반환
            return BitmapImageFromMemoryStream(stream);
        }


        public static void CopyBitmapImageWithPads(WriteableBitmap dstImage, BitmapSource srcimg,
                                                   int leftPad, int topPad, int rightPad, int bottomPad)
        {
            // 복사될 Rect 계산
            var srcRect = new Int32Rect { X = (leftPad < 0) ? -leftPad : 0, Y = (topPad < 0) ? -topPad : 0 };
            srcRect.Width = ((rightPad < 0) ? srcimg.PixelWidth + rightPad : srcimg.PixelWidth) - srcRect.X;
            srcRect.Height = ((bottomPad < 0) ? srcimg.PixelHeight + bottomPad : srcimg.PixelHeight) - srcRect.Y;

            // 복사받을 Rect 계산
            var destRect = new Int32Rect { X = (leftPad > 0) ? leftPad : 0, Y = (topPad > 0) ? topPad : 0 };
            destRect.Width = ((rightPad > 0) ? dstImage.PixelWidth - rightPad : dstImage.PixelWidth) - destRect.X;
            destRect.Height = ((bottomPad > 0) ? dstImage.PixelHeight - bottomPad : dstImage.PixelHeight) - destRect.Y;

            // 임시 배열 생성, stride 계산 및 데이터 복사
            var copiedArray = new byte[srcRect.Width * srcRect.Height * ((srcimg.Format.BitsPerPixel + 7) / 8)];
            int stride = srcRect.Width * ((srcimg.Format.BitsPerPixel + 7) / 8);
            srcimg.CopyPixels(srcRect, copiedArray, stride, 0);
            dstImage.WritePixels(destRect, copiedArray, stride, 0);
        }


        public static void Resize1bpp(Bitmap srcBitmap, Bitmap dstBitmap)
        {
            int srcWidth = srcBitmap.Width;
            int srcHeight = srcBitmap.Height;
            int dstWidth = dstBitmap.Width;
            int dstHeight = dstBitmap.Height;

            // LockBits
            BitmapData srcData = srcBitmap.LockBits(new System.Drawing.Rectangle(0, 0, srcWidth, srcHeight),
                ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);
            BitmapData dstData = dstBitmap.LockBits(new System.Drawing.Rectangle(0, 0, dstWidth, dstHeight),
                ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

            unsafe
            {
                byte* srcBasePtr = (byte*)srcData.Scan0.ToPointer();
                byte* dstBasePtr = (byte*)dstData.Scan0.ToPointer();

                // Nearest Neighbor
                for(int dstY = 0; dstY < dstHeight; dstY++)
                {
                    // Source Y 계산
                    int srcY = (int)((dstY + 0.5) * srcHeight / dstHeight);
                    if(srcY < 0) srcY = 0;
                    if(srcY >= srcHeight) srcY = srcHeight - 1;

                    // Source, Destication Y Stride offset 계산
                    int srcByteStrideOffset = srcY * srcData.Stride;
                    int dstByteStrideOffset = dstY * dstData.Stride;

                    for(int dstX = 0; dstX < dstWidth; dstX++)
                    {
                        // Source X 계산
                        int srcX = (int)((dstX + 0.5) * srcWidth / dstWidth);
                        if(srcX < 0) srcX = 0;
                        if(srcX >= srcWidth) srcX = srcWidth - 1;

                        // Source Pixel value 따기
                        byte b = srcBasePtr[srcByteStrideOffset + (srcX / 8)];
                        byte bitVal = (byte)((b >> (7 - (srcX % 8))) & 1);

                        // Destination Pixel value 넣기
                        byte mask = (byte)(1 << (7 - (dstX % 8)));
                        int dstByteIndex = dstY * dstData.Stride + (dstX / 8);

                        if(bitVal == 0) dstBasePtr[dstByteIndex] &= (byte)~mask;
                        else dstBasePtr[dstByteIndex] |= mask;
                    }
                }
            }

            // UnlockBits
            srcBitmap.UnlockBits(srcData);
            dstBitmap.UnlockBits(dstData);
        }

        public static byte[] ToArray(this WriteableBitmap reachable)
        {
            reachable.Lock();
            int bufferSize = reachable.BackBufferStride * reachable.PixelHeight;
            byte[] buffer = new byte[bufferSize];
            Marshal.Copy(reachable.BackBuffer, buffer, 0, bufferSize);
            reachable.Unlock();
            return buffer;
        }

        public static void FromArray(this WriteableBitmap reachable, byte[] buffer)
        {
            reachable.Lock();
            int bufferSize = reachable.BackBufferStride * reachable.PixelHeight;
            Marshal.Copy(buffer, 0, reachable.BackBuffer, bufferSize);
            reachable.AddDirtyRect(new Int32Rect(0, 0, reachable.PixelWidth, reachable.PixelHeight));
            reachable.Unlock();
        }

        public static void FromArray(this WriteableBitmap reachable, IntPtr buffer)
        {
            reachable.Lock();
            int bufferSize = reachable.BackBufferStride * reachable.PixelHeight;
            unsafe { Buffer.MemoryCopy((void*)buffer, (void*)reachable.BackBuffer, bufferSize, bufferSize); }
            reachable.AddDirtyRect(new Int32Rect(0, 0, reachable.PixelWidth, reachable.PixelHeight));
            reachable.Unlock();
        }
    }
}