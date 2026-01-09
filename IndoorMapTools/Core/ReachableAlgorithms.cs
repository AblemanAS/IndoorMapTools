
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bitmap = System.Drawing.Bitmap;

namespace IndoorMapTools.Core
{
    public class ReachableAlgorithms
    {
        private const int BRTH_ABS = 50;
        private const int BRTH_REL = 20;
        private readonly static Color UNMARK_COLOR = Color.FromArgb(0, 0, 0, 0);
        private readonly static Color MARK_COLOR = Color.FromArgb(200, 230, 230, 255);
        private readonly static System.Drawing.Color UNMARK_COLOR_DRAW = System.Drawing.Color.FromArgb(0, 0, 0, 0);
        private readonly static System.Drawing.Color MARK_COLOR_DRAW = System.Drawing.Color.FromArgb(200, 230, 230, 255);
        private readonly static BitmapPalette PALETTE_INDEX = new BitmapPalette(new List<Color> { UNMARK_COLOR, MARK_COLOR });

        /// <summary>
        /// 주어진 이미지 규격, DPI에 해당하는 Reachable 작업영역 이미지를 생성
        /// </summary>
        public static WriteableBitmap CreateReachable(int imageWidth, int imageHeight, double dpiX = 96, double dpiY = 96)
        {
            WriteableBitmap result = null;
            Application.Current.Dispatcher.Invoke(() =>
                result = new WriteableBitmap(imageWidth, imageHeight, dpiX, dpiY, PixelFormats.Indexed1, PALETTE_INDEX));
            return result;
        }

        /// <summary>
        /// Single Mark의 flood fill 수행에 필요한 Gray Scale 룩업 캐시 생성
        /// </summary>
        /// <param name="mapImage">원본 Map Image</param>
        /// <returns>출력 Gray Scale 룩업 캐시</returns>
        public static byte[] GetGrayArray(BitmapImage mapImage)
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
            return grayArray;
        }

        /// <summary>
        /// 대상 Reachable 작업 영역 내의 정해진 위치에 주어진 값으로 마킹. 
        /// Gray Scale 룩업 캐시를 참조하여 flood fill을 수행, 페인트 도구처럼 작동함.
        /// </summary>
        /// <param name="reachable">Rechable 작업영역 이미지</param>
        /// <param name="grayArray">Gray Scale 룩업 캐시 (GetGrayArray로 생성 필요)</param>
        /// <param name="bitSet">true / false 중 무엇으로 마킹할 것인지</param>
        /// <param name="targetInImage">DPI가 적용된 맵 이미지 상의 픽셀 좌표 위치</param>
        public static void SingleMark(WriteableBitmap reachable, byte[] grayArray, bool bitSet, Point targetInImage)
        {
            // 지정된 점을 대상으로 FloodFill 호출
            if(targetInImage.X < 0 || targetInImage.X >= reachable.PixelWidth ||
                targetInImage.Y < 0 || targetInImage.Y >= reachable.PixelHeight) return;

            reachable.Lock();

            unsafe
            {
                FloodFill(grayArray, (byte*)reachable.BackBuffer.ToPointer(),
                    reachable.PixelWidth, reachable.PixelHeight, reachable.BackBufferStride,
                    bitSet, ((int)targetInImage.X, (int)targetInImage.Y));
            }

            reachable.AddDirtyRect(new Int32Rect(0, 0, reachable.PixelWidth, reachable.PixelHeight));
            reachable.Unlock();
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
            var markTargetQueue = new Queue<(int, int)>(); // x, y
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

            // 비트연산을 위한 로컬 함수 정의

            bool IsBitSet(byte* basePtr, int stride, int x, int y)
                => (basePtr[(y * stride) + (x / 8)] & ((byte)(1 << (7 - x % 8)))) != 0;

            void SetBit(byte* basePtr, int stride, int x, int y)
                => basePtr[(y * stride) + (x / 8)] |= (byte)(1 << (7 - x % 8));

            void ClearBit(byte* basePtr, int stride, int x, int y)
                => basePtr[(y * stride) + (x / 8)] &= (byte)~(1 << (7 - x % 8));
        }

        /// <summary>
        /// 대상 Reachable 작업 영역 내의 정해진 영역에 주어진 값으로 마킹. 
        /// </summary>
        /// <param name="reachable">Rechable 작업영역 이미지</param>
        /// <param name="bitSet">true / false 중 무엇으로 마킹할 것인지</param>
        /// <param name="targetRegion">마킹 대상 ROI 영역</param>
        public static void RegionMark(WriteableBitmap reachable, bool bitSet, Rect targetRegion)
        {
            int imageWidth = reachable.PixelWidth;
            int imageHeight = reachable.PixelHeight;

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


        public static WriteableBitmap GetPadCropReachable(WriteableBitmap original, int leftPad, int topPad, int rightPad, int bottomPad)
        {
            var newReachable = CreateReachable(original.PixelWidth + leftPad + rightPad, original.PixelHeight + topPad + bottomPad);
            ImageAlgorithms.CopyBitmapImageWithPads(newReachable, original, leftPad, topPad, rightPad, bottomPad);
            return newReachable;
        }



        /// <summary>
        /// Reachable 작업영역을 OGM으로 래스터화
        /// </summary>
        /// <param name="reachable">Reachable 작업영역</param>
        /// <param name="rotation">이미지 회전값</param>
        /// <param name="scale">래스터화 출력 배율</param>
        /// <param name="conservative">래스터 정책</param>
        /// <param name="includedPolygons">포함할 폴리곤 영역</param>
        /// <param name="progressCb">진행률 보고</param>
        /// <returns>출력 OGM</returns>
        public static Bitmap BuildOGMfromReachable(WriteableBitmap reachable, double rotation, double scale,
            bool conservative, IEnumerable<Point[]> includedPolygons, IProgress<int> progressCb = null)
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
                progressCb?.Report(10);

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
                progressCb?.Report(30);

                timer.Stop();
                Console.WriteLine($"Polygons : {timer.ElapsedMilliseconds}");
                timer.Reset();
                timer.Start();

                // 비트맵 회전 및 뒤집기
                rotateFlippedBitmap = ImageAlgorithms.GetRotatedBitmap(originalBitmap, rotation, true);

                timer.Stop();
                Console.WriteLine($"GetRotatedBitmap : {timer.ElapsedMilliseconds}");
                
                rotateFlippedBitmap.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);
                progressCb?.Report(35);

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
                    progressCb?.Report(40);
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
                            progressCb?.Report(lastProgress = currentProgress);
                    }
                }
                else // 수용적 메카니즘
                {
                    progressCb?.Report(40);
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
                            progressCb?.Report(currentProgress);
                        }
                    }
                }

                timer.Stop();
                Console.WriteLine($"OGM flag : {timer.ElapsedMilliseconds}");

                // 랜드마크 중점 OGM 반영
                // 원본 이미지 기준 Reachable 상단 손실폭 계산
                var transformer = MathAlgorithms.CalculateTransformer(originalBitmap.Width, originalBitmap.Height, rotation, scale);
                foreach(var polygon in includedPolygons)
                {
                    // 최종 scaledBitmap에 해당하는 cell의 값을 true로 할당해 주기 위해,
                    // pixels 배열에 해당하는 위치의 비트를 1로 설정
                    // scaledBitmap 생성 시의 상단 손실폭을 고려하여 y좌표를 보정
                    Point calculatedCenter = MathAlgorithms.CalculatePolygonCenter(polygon); // Center
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

            progressCb?.Report(100);

            return scaledBitmap;
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

        /// <summary>
        /// OGM을 Reachable 작업영역으로 변환
        /// </summary>
        /// <param name="ogm">원본 OGM</param>
        /// <param name="pixelWidth">출력 Reachable Width</param>
        /// <param name="pixelHeight">출력 Reachable Height</param>
        /// <param name="progressCb">진행률 보고</param>
        /// <returns>출력 Reachable 작업영역</returns>
        public static WriteableBitmap BuildReachablefromOGM(Bitmap ogm, int pixelWidth, int pixelHeight, Action<int> progressCb = null)
        {
            // 상하 뒤집기
            ogm.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);
            progressCb?.Invoke(10);

            // 이미지 스케일
            var scaledBitmap = new Bitmap(pixelWidth, pixelHeight, System.Drawing.Imaging.PixelFormat.Format1bppIndexed);
            ImageAlgorithms.Resize1bpp(ogm, scaledBitmap);
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
    }
}
