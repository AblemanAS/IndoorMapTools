
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bitmap = System.Drawing.Bitmap;

namespace IndoorMapTools.Core
{
    public static class ReachableModule
    {
        private const int BRTH_ABS = 50;
        private const int BRTH_REL = 20;
        private readonly static Color UNMARK_COLOR = Color.FromArgb(0, 0, 0, 0);
        private readonly static Color MARK_COLOR = Color.FromArgb(200, 230, 230, 255);
        private readonly static System.Drawing.Color UNMARK_COLOR_DRAW = System.Drawing.Color.FromArgb(0, 0, 0, 0);
        private readonly static System.Drawing.Color MARK_COLOR_DRAW = System.Drawing.Color.FromArgb(200, 230, 230, 255);
        private readonly static BitmapPalette PALETTE_INDEX = new BitmapPalette(new List<Color> { UNMARK_COLOR, MARK_COLOR });

        // 마킹 알고리즘 시 참조할 배열 풀
        private static readonly ConditionalWeakTable<BitmapImage, byte[]> grayArrayTable = new ConditionalWeakTable<BitmapImage, byte[]>();

        // 마킹 알고리즘 큐
        private static readonly Queue<(int, int)> markTargetQueue = new Queue<(int, int)>(); // x, y

        public static WriteableBitmap CreateReachable(int imageWidth, int imageHeight, double dpiX = 96, double dpiY = 96)
        {
            WriteableBitmap result = null;
            Application.Current.Dispatcher.Invoke(() =>
                result = new WriteableBitmap(imageWidth, imageHeight, dpiX, dpiY, PixelFormats.Indexed1, PALETTE_INDEX));
            return result;
        }

        public static void SingleMark(BitmapImage mapImage, WriteableBitmap reachable, bool bitSet, Point targetInImage)
        {
            // 지정된 점을 대상으로 FloodFill 호출
            if(targetInImage.X < 0 || targetInImage.X >= mapImage.PixelWidth ||
                targetInImage.Y < 0 || targetInImage.Y >= mapImage.PixelHeight) return;

            reachable.Lock();

            unsafe
            {
                FloodFill(GetArray(mapImage), (byte*)reachable.BackBuffer.ToPointer(),
                    mapImage.PixelWidth, mapImage.PixelHeight, reachable.BackBufferStride,
                    bitSet, ((int)targetInImage.X, (int)targetInImage.Y));
            }

            reachable.AddDirtyRect(new Int32Rect(0, 0, reachable.PixelWidth, reachable.PixelHeight));
            reachable.Unlock();
        }


        public static void RegionMark(BitmapImage mapImage, WriteableBitmap reachable, bool bitSet, Rect targetRegion)
        {
            int imageWidth = mapImage.PixelWidth;
            int imageHeight = mapImage.PixelHeight;

            // 범위 Top, Left, Bottom, Right 좌표 얻고, 이미지 범위 내로 좌표 제한
            int left = Math.Max((int)Math.Round(targetRegion.Left), 0);
            int top = Math.Max((int)Math.Round(targetRegion.Top), 0);
            int right = Math.Min((int)Math.Round(targetRegion.Right), imageWidth - 1);
            int bottom = Math.Min((int)Math.Round(targetRegion.Bottom), imageHeight - 1);

            reachable.Lock();

            unsafe
            {
                byte* basePtr = (byte*)reachable.BackBuffer.ToPointer();
                int stride = reachable.BackBufferStride;

                // Offset, Mask 계산
                int leftOffset = (left / 8);
                int rightOffset = (right / 8);
                byte leftMask = (byte)(0b_1111_1111 >> (left % 8));
                byte rightMask = (byte)(0b_1111_1111 << ((7 - right % 8)));

                // 시작 바이트 오프셋 = 끝 바이트 오프셋일 경우
                if(leftOffset == rightOffset)
                {
                    byte batchValue = (byte)(leftMask & rightMask);
                    if(bitSet) // Set일 경우
                    {
                        for(int y = top; y <= bottom; y++)
                            basePtr[y * stride + leftOffset] |= batchValue;
                    }
                    else // Clear일 경우
                    {
                        batchValue = (byte)~batchValue; // 마스크 뒤집기
                        for(int y = top; y <= bottom; y++)
                            basePtr[y * stride + leftOffset] &= batchValue;
                    }
                }
                else // 그 외의 경우
                {
                    if(bitSet) // Set일 경우
                    {
                        for(int y = top; y <= bottom; y++)
                        {
                            int rowOffset = y * stride;
                            basePtr[rowOffset + leftOffset] |= leftMask; // 시작 바이트
                            for(int xBytePos = leftOffset + 1; xBytePos < rightOffset; xBytePos++)
                                basePtr[rowOffset + xBytePos] = 0b_1111_1111;
                            basePtr[rowOffset + rightOffset] |= rightMask; // 끝 바이트
                        }
                    }
                    else // Clear일 경우
                    {
                        leftMask = (byte)~leftMask; rightMask = (byte)~rightMask; // 마스크 뒤집기

                        for(int y = top; y <= bottom; y++)
                        {
                            int rowOffset = y * stride;
                            basePtr[rowOffset + leftOffset] &= leftMask; // 시작 바이트
                            for(int xBytePos = leftOffset + 1; xBytePos < rightOffset; xBytePos++)
                                basePtr[rowOffset + xBytePos] = 0b_0000_0000;
                            basePtr[rowOffset + rightOffset] &= rightMask; // 끝 바이트
                        }
                    }
                }
            }

            // Reachable 이미지에 갱신
            reachable.AddDirtyRect(new Int32Rect(left, top, (right - left + 1), (bottom - top + 1)));
            reachable.Unlock();
        }


        public static byte[] GetArray(BitmapImage mapImage)
        {
            if(grayArrayTable.TryGetValue(mapImage, out byte[] array)) return array;
            else
            {
                // Reachable 마킹용 참조 맵 그레이스케일 이미지 변환 및 초기화
                FormatConvertedBitmap grayImage = new FormatConvertedBitmap();
                grayImage.BeginInit();
                grayImage.Source = mapImage;
                grayImage.DestinationFormat = PixelFormats.Gray8;
                grayImage.EndInit();

                // 참조 바이트 배열
                byte[] grayArray = new byte[grayImage.PixelHeight * grayImage.PixelWidth];
                grayImage.CopyPixels(grayArray, grayImage.PixelWidth, 0); // 원본 이미지로부터 데이터 미러
                grayArrayTable.Add(mapImage, grayArray); // 풀에 등록
                return grayArray;
            }
        }

        private static unsafe void FloodFill(byte[] grayArray, byte* reachableBufferPtr,
            int imageWidth, int imageHeight, int bufferStride, bool value, (int X, int Y) targetInImage)
        {
            // 픽셀 범위 검사
            if(targetInImage.X < 0 || targetInImage.X >= imageWidth ||
                targetInImage.Y < 0 || targetInImage.Y >= imageHeight) return;

            // 픽셀 데이터 검사
            if(IsBitSet(reachableBufferPtr, bufferStride, targetInImage.X, targetInImage.Y) == value) return;

            // 출발점 픽셀 밝기
            byte targetBrightness = grayArray[targetInImage.Y * imageWidth + targetInImage.X];

            // 큐에 해당 픽셀 넣으며 알고리즘 시작
            markTargetQueue.Enqueue(targetInImage);

            if(value)
            {
                while(markTargetQueue.Count > 0)
                {
                    var (targetX, targetY) = markTargetQueue.Dequeue();             // 큐에서 대상을 하나 꺼내고
                    if(targetX < 0 || targetX >= imageWidth || targetY < 0 || targetY >= imageHeight) continue;  // 픽셀 범위 검사
                    if(IsBitSet(reachableBufferPtr, bufferStride, targetX, targetY)) continue; // 픽셀 데이터 검사
                    byte curBrightness = grayArray[targetY * imageWidth + targetX];
                    if(curBrightness > BRTH_ABS && (Math.Abs(targetBrightness - curBrightness) < BRTH_REL))
                    {
                        SetBit(reachableBufferPtr, bufferStride, targetX, targetY); // 값 대입
                        // 큐에 다음 대상 값 추가
                        markTargetQueue.Enqueue((targetX - 1, targetY));
                        markTargetQueue.Enqueue((targetX, targetY - 1));
                        markTargetQueue.Enqueue((targetX + 1, targetY));
                        markTargetQueue.Enqueue((targetX, targetY + 1));
                    }
                }
            }
            else
            {
                while(markTargetQueue.Count > 0)
                {
                    var (targetX, targetY) = markTargetQueue.Dequeue();             // 큐에서 대상을 하나 꺼내고
                    if(targetX < 0 || targetX >= imageWidth || targetY < 0 || targetY >= imageHeight) continue;  // 픽셀 범위 검사
                    if(!IsBitSet(reachableBufferPtr, bufferStride, targetX, targetY)) continue; // 픽셀 데이터 검사
                    byte curBrightness = grayArray[targetY * imageWidth + targetX];

                    if(curBrightness > BRTH_ABS && (Math.Abs(targetBrightness - curBrightness) < BRTH_REL))
                    {
                        ClearBit(reachableBufferPtr, bufferStride, targetX, targetY); // 값 대입
                        // 큐에 다음 대상 값 추가
                        markTargetQueue.Enqueue((targetX - 1, targetY));
                        markTargetQueue.Enqueue((targetX, targetY - 1));
                        markTargetQueue.Enqueue((targetX + 1, targetY));
                        markTargetQueue.Enqueue((targetX, targetY + 1));
                    }
                }
            }
        }


        private static unsafe bool IsBitSet(byte* basePtr, int stride, int x, int y)
            => (basePtr[(y * stride) + (x / 8)] & ((byte)(1 << (7 - x % 8)))) != 0;

        private static unsafe void SetBit(byte* basePtr, int stride, int x, int y)
            => basePtr[(y * stride) + (x / 8)] |= (byte)(1 << (7 - x % 8));

        private static unsafe void ClearBit(byte* basePtr, int stride, int x, int y)
            => basePtr[(y * stride) + (x / 8)] &= (byte)~(1 << (7 - x % 8));


        public static Bitmap BuildOGMfromReachable(WriteableBitmap reachable, double rotation, double scale,
            bool conservative, IEnumerable<Point[]> includedPolygons, Action<int> progressCb = null)
        {
            Bitmap originalBitmap = null, rotateFlippedBitmap = null, scaledBitmap = null;
            BitmapData bitmapData = null;

            try
            {
                Stopwatch timer = new Stopwatch();
                timer.Start();

                // 원본 Reachable 비트맵 추출
                WriteableBitmap frozenReachable = null;
                reachable.Dispatcher.Invoke(() => frozenReachable = (WriteableBitmap)reachable.GetAsFrozen());
                originalBitmap = ToBitmapFrom1bpp(frozenReachable);
                progressCb?.Invoke(10);

                timer.Stop();
                Console.WriteLine($"ToBitmap : {timer.ElapsedMilliseconds}");
                timer.Reset();
                timer.Start();

                // 랜드마크 그리기
                using(var g = System.Drawing.Graphics.FromImage(originalBitmap))
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;

                    foreach(var polygon in includedPolygons)
                    {
                        var polygonF = new List<System.Drawing.PointF>();
                        foreach(Point curPoint in polygon)
                            polygonF.Add(new System.Drawing.PointF((float)curPoint.X, (float)curPoint.Y));
                        g.FillPolygon(System.Drawing.Brushes.White, polygonF.ToArray());
                    }
                }
                progressCb?.Invoke(30);

                timer.Stop();
                Console.WriteLine($"Polygons : {timer.ElapsedMilliseconds}");
                timer.Reset();
                timer.Start();

                // 비트맵 회전 및 뒤집기
                rotateFlippedBitmap = ImageModule.GetRotatedBitmap(originalBitmap, rotation, true);

                timer.Stop();
                Console.WriteLine($"GetRotatedBitmap : {timer.ElapsedMilliseconds}");
                
                rotateFlippedBitmap.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);
                progressCb?.Invoke(35);

                timer.Reset();
                timer.Start();

                // OGM 기록할 축소 비트맵 생성, (int)로 floorin 되므로 scale 대비 약간 작게 생성됨
                int newWidth = (int)(rotateFlippedBitmap.Width * scale);
                int newHeight = (int)(rotateFlippedBitmap.Height * scale);
                scaledBitmap = new Bitmap(newWidth, newHeight, System.Drawing.Imaging.PixelFormat.Format1bppIndexed);
                scaledBitmap.SetResolution(96, 96);

                // 축소 비트맵 비트 락
                bitmapData = scaledBitmap.LockBits(new System.Drawing.Rectangle(0, 0, newWidth, newHeight),
                    ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format1bppIndexed);

                byte[] pixels = new byte[bitmapData.Stride * newHeight];
                int lastProgress = 40;

                if(conservative) // 보수적 메카니즘
                {
                    for(int i = 0; i < pixels.Length; i++) pixels[i] = 255;
                    progressCb?.Invoke(40);
                    for(int x = 0; x < rotateFlippedBitmap.Width; x++)
                    {
                        for(int y = 0; y < rotateFlippedBitmap.Height; y++)
                        {
                            //Console.WriteLine($"{x}, {y} : {rotateFlippedBitmap.GetPixel(x, y)}");
                            if(rotateFlippedBitmap.GetPixel(x, y).A == 0)
                            {
                                int targetYPos = (int)((y + 0.5) * scale);
                                if(targetYPos >= newHeight) continue;
                                int targetXPos = (int)((x + 0.5) * scale);
                                pixels[targetYPos * bitmapData.Stride + targetXPos / 8] &= (byte)~(0x80 >> (targetXPos % 8));
                            }
                        }

                        var currentProgress = x * 60 / rotateFlippedBitmap.Width + 40;
                        if(currentProgress > lastProgress)
                            progressCb?.Invoke(lastProgress = currentProgress);
                    }
                }
                else // 수용적 메카니즘
                {
                    progressCb?.Invoke(40);
                    for(int x = 0; x < rotateFlippedBitmap.Width; x++)
                    {
                        for(int y = 0; y < rotateFlippedBitmap.Height; y++)
                        {
                            if(rotateFlippedBitmap.GetPixel(x, y).A != 0)
                            {
                                int targetYPos = (int)((y + 0.5) * scale);
                                if(targetYPos >= newHeight) continue;
                                int targetXPos = (int)((x + 0.5) * scale);
                                pixels[targetYPos * bitmapData.Stride + targetXPos / 8] |= (byte)(0x80 >> (targetXPos % 8));
                            }
                        }

                        var currentProgress = x * 60 / rotateFlippedBitmap.Width + 40;
                        if(currentProgress > lastProgress)
                        {
                            lastProgress = currentProgress;
                            progressCb?.Invoke(currentProgress);
                        }
                    }
                }

                timer.Stop();
                Console.WriteLine($"OGM flag : {timer.ElapsedMilliseconds}");

                // 랜드마크 중점 OGM 반영
                // 원본 이미지 기준 Reachable 상단 손실폭 계산
                var transformer = MathModule.CalculateTransformer(originalBitmap.Width, originalBitmap.Height, rotation, scale);
                foreach(var polygon in includedPolygons)
                {
                    // 최종 scaledBitmap에 해당하는 cell의 값을 true로 할당해 주기 위해,
                    // pixels 배열에 해당하는 위치의 비트를 1로 설정
                    // scaledBitmap 생성 시의 상단 손실폭을 고려하여 y좌표를 보정
                    Point calculatedCenter = MathModule.CalculatePolygonCenter(polygon); // Center
                    Point transformedLoc = transformer.Transform(calculatedCenter);      // Location

                    int targetX = (int)transformedLoc.X;
                    int targetY = (int)transformedLoc.Y;

                    // 경계 Assert
                    Debug.Assert(targetX >= 0 && targetX < newWidth && targetY >= 0 && targetY < newHeight,
                        $"Transformed location out of bounds: {targetX}, {targetY}");

                    int byteIndex = targetY * bitmapData.Stride + (targetX / 8);
                    byte mask = (byte)(0b10000000 >> (targetX % 8));
                    pixels[byteIndex] |= mask;
                }

                Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
            }
            finally
            {
                scaledBitmap.UnlockBits(bitmapData);
                rotateFlippedBitmap.Dispose();
                originalBitmap.Dispose();
            }

            progressCb?.Invoke(100);

            return scaledBitmap;
        }


        public static WriteableBitmap BuildReachablefromOGM(Bitmap ogm, int pixelWidth, int pixelHeight, Action<int> progressCb = null)
        {
            // 상하 뒤집기
            ogm.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);
            progressCb?.Invoke(10);

            // 이미지 스케일
            var scaledBitmap = new Bitmap(pixelWidth, pixelHeight, System.Drawing.Imaging.PixelFormat.Format1bppIndexed);
            ImageModule.Resize1bpp(ogm, scaledBitmap);
            ogm.Dispose();
            progressCb?.Invoke(20);

            // 새 Reachable 생성
            var result = CreateReachable(pixelWidth, pixelHeight);

            // 데이터 복사
            BitmapData bitmapData = scaledBitmap.LockBits(new System.Drawing.Rectangle(0, 0, scaledBitmap.Width, scaledBitmap.Height),
                ImageLockMode.ReadOnly, scaledBitmap.PixelFormat);
            result.Dispatcher.Invoke(() => result.FromArray(bitmapData.Scan0));
            scaledBitmap.UnlockBits(bitmapData);
            scaledBitmap.Dispose();
            progressCb?.Invoke(100);

            return result;
        }

        private static unsafe Bitmap ToBitmapFrom1bpp(BitmapSource source)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = (width + 7) / 8;

            byte[] pixelData = new byte[stride * height];
            source.CopyPixels(pixelData, stride, 0);

            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            BitmapData bmpData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            int* dst = (int*)bmpData.Scan0;

            int dstStridePixels = bmpData.Stride / 4;

            for(int y = 0; y < height; y++)
            {
                for(int x = 0; x < width; x++)
                {
                    int byteIndex = y * stride + x / 8;
                    int bitIndex = 7 - (x % 8);
                    bool isWhite = (pixelData[byteIndex] & (1 << bitIndex)) != 0;

                    dst[y * dstStridePixels + x] = isWhite ? unchecked((int)0xFFFFFFFF) : 0x00000000;
                }
            }

            bitmap.UnlockBits(bmpData);
            bitmap.SetResolution(96, 96);
            return bitmap;
        }

    }
}
