
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Bitmap = System.Drawing.Bitmap;
using Vector = System.Numerics.Vector;

namespace IndoorMapTools.Core
{
    public class ReachableAlgorithms
    {
        private const byte BRTH_ABS = 150;
        private const byte BRTH_REL = 20;
        private readonly static Vector<byte> BRTH_ABS_VEC = new(BRTH_ABS);
        private readonly static Vector<byte> BRTH_REL_VEC = new(BRTH_REL);
        private readonly static Color UNMARK_COLOR = Color.FromArgb(0, 0, 0, 0);
        private readonly static Color MARK_COLOR = Color.FromArgb(200, 230, 230, 255);
        private readonly static System.Drawing.Color UNMARK_COLOR_DRAW = System.Drawing.Color.FromArgb(0, 0, 0, 0);
        private readonly static System.Drawing.Color MARK_COLOR_DRAW = System.Drawing.Color.FromArgb(200, 230, 230, 255);
        private readonly static BitmapPalette PALETTE_INDEX = new(new List<Color> { UNMARK_COLOR, MARK_COLOR });


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
        /// <para>Flood Fill 가속을 위해 64 byte의 배수 stride로 생성됨</para>
        /// </summary>
        /// <param name="mapImage">원본 Map Image</param>
        /// <returns>출력 Gray Scale 룩업 캐시</returns>
        public static byte[] GetGrayArray(BitmapImage mapImage)
        {
            // Reachable 마킹용 참조 맵 그레이스케일 이미지 변환 및 초기화
            var grayImage = new FormatConvertedBitmap();
            grayImage.BeginInit();
            grayImage.Source = mapImage;
            grayImage.DestinationFormat = PixelFormats.Gray8;
            grayImage.EndInit();

            // 참조 바이트 배열
            int imageWidth = grayImage.PixelWidth;
            int imageHeight = grayImage.PixelHeight;
            int stride = (imageWidth + 63) & ~63;
            byte[] grayArray = new byte[imageHeight * stride];
            grayImage.CopyPixels(grayArray, stride, 0); // 원본 이미지로부터 데이터 미러

            // 우측 패딩 zero fill
            int clearWidth = stride - imageWidth;
            for(int y = 0; y < imageHeight; y++)
            {
                int zerofillOffset = y * stride + imageWidth;
                grayArray.AsSpan(zerofillOffset, clearWidth).Clear();
            }
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

            Int32Rect dirty;

            reachable.Lock();
            unsafe
            {
                dirty = FloodFill(grayArray, (byte*)reachable.BackBuffer.ToPointer(),
                    reachable.PixelWidth, reachable.PixelHeight, reachable.BackBufferStride,
                    bitSet, ((int)targetInImage.X, (int)targetInImage.Y));
            }

            if(!dirty.IsEmpty) reachable.AddDirtyRect(dirty);
            reachable.Unlock();
        }


        //private enum FloodFillPhase { BitHead, ByteHead, Body, ByteTail, BitTail, Finished }

        // TODO : Vector의 Count 32, 16, fallback 기준으로 각각 짜야 함
        // ## Scanline logic
        // 64byte 단위로 interval 처리 로직
        // buffer : 기존대로 ulong 단위로 읽어 (64bit) all clear 확인
        // grayarray: 처음 클릭 위치의 interval 찾고, Vector 로 all 확인
        // all clear 시 즉시 달리고, all not clear 시 ulong -> byte로 fallback
        // 최종 fallback 기준에서, bit fallback, byte fallback에 따라 
        // bit head로 시작하는지 byte head로 시작하는지 구분하여 처리
        // bit head - byte head - body (= interval run) - byte tail - bit tail 로 fill 하며 left, right 찾기
        // ## Branch sampling logic
        // left, right로, 윗줄, 아랫줄에 대해 동일 방식 phase 기반 처리
        // gray array의 경우 64 all clear -> Vector all, 8 all clear -> ulong MAX/0, 1 clear -> byte lookup


        //// SIMD32 기준
        //private static unsafe Int32Rect FloodFill(byte[] grayArray, byte* reachableBufferPtr,
        //    int imageWidth, int imageHeight, int bufferStride, bool value, (int X, int Y) targetInImage)
        //{
        //    // 픽셀 범위 검사
        //    if(targetInImage.X < 0 || targetInImage.X >= imageWidth ||
        //        targetInImage.Y < 0 || targetInImage.Y >= imageHeight) return Int32Rect.Empty;

        //    // 픽셀 데이터 검사
        //    if(isBitSet(reachableBufferPtr, targetInImage.Y * bufferStride, targetInImage.X) == value) return Int32Rect.Empty;

        //    // Gray Array stride 계산
        //    int arrayStride = (imageWidth + 63) & ~63;

        //    // 출발점 픽셀 밝기 확인
        //    byte targetBrightness = grayArray[targetInImage.Y * arrayStride + targetInImage.X];
        //    if(targetBrightness < BRTH_ABS) return Int32Rect.Empty;

        //    // 해당 픽셀 칠하고 큐에 해당 픽셀 넣으며 알고리즘 시작
        //    if(value) setBit(reachableBufferPtr, targetInImage.Y * bufferStride, targetInImage.X);
        //    else clearBit(reachableBufferPtr, targetInImage.Y * bufferStride, targetInImage.X);
        //    var markTargetStack = new Stack<(int, int)>(512); // x, y
        //    markTargetStack.Push(targetInImage);

        //    // dirty rect
        //    int minX = int.MaxValue;
        //    int minY = int.MaxValue;
        //    int maxX = int.MinValue;
        //    int maxY = int.MinValue;

        //    // 탐색 관련 변수
        //    FloodFillPhase state;
        //    int ptr;
        //    int left, right;
        //    int startX;
        //    int targetUpperY, targetLowerY;
        //    int bufferOffsetY, bufferOffsetUpperY, bufferOffsetLowerY;
        //    int arrayOffsetY, arrayOffsetUpperY, arrayOffsetLowerY;
        //    int headCount;
        //    int headBufferOffset;
        //    int headArrayOffset;
        //    int headBufferOffsetX = 0;

        //    var targetBrVec = new Vector<byte>(targetBrightness);

        //    // scanline floodfill
        //    if(value) // true (= 1) 로 칠할 때
        //    {
        //        while(markTargetStack.Count > 0)
        //        {
        //            var (targetX, targetY) = markTargetStack.Pop(); // 스택에서 대상을 하나 꺼내고

        //            bufferOffsetY = targetY * bufferStride;
        //            arrayOffsetY = targetY * arrayStride;

        //            int intervalOffset = arrayOffsetY + targetX & ~63; // 초기 인터벌 오프셋

        //            /*** 왼쪽으로 탐색 ***/
        //            state = FloodFillPhase.BitHead;
        //            while(state != FloodFillPhase.Finished)
        //            {
        //                switch(state)
        //                {
        //                    case FloodFillPhase.BitHead:
        //                        startX = targetX; // Bit head 시작 X 좌표 (비트 단위)
        //                        headBufferOffsetX = startX / 8; // Bit head 끝 X 오프셋
        //                        headCount = startX & 7; // Bit head 의 총 비트수
        //                        headBufferOffset = bufferOffsetY + headBufferOffsetX; // Bit head 끝의 버퍼 오프셋
        //                        headArrayOffset = arrayOffsetY + startX - headCount; // Bit head 끝의 어레이 오프셋
        //                        for(ptr = headCount - 1; ptr >= 0; ptr--) // ptr : bit 단위
        //                        {
        //                            if(isBitSetNew(reachableBufferPtr, headBufferOffset, ptr)) break; // 픽셀 데이터 검사
        //                            if(!isBrightnessValid(grayArray, targetBrightness, headArrayOffset, ptr)) break; // 밝기 검사
        //                            setBitNew(reachableBufferPtr, headBufferOffset, ptr);
        //                        }
        //                        ptr++; // 한 칸 돌아가기

        //                        if(ptr == 0) state = FloodFillPhase.ByteHead; // Byte head 로 전환
        //                        else // Bit head 내에 left가 있음 (Byte head 및 Body가 없음)
        //                        { 
        //                            left = startX & ~7 + ptr;
        //                            state = FloodFillPhase.Finished;
        //                        }
        //                        break;

        //                    case FloodFillPhase.ByteHead:
        //                        startX = headBufferOffsetX; // Byte head X 좌표 (바이트단위)
        //                        headBufferOffsetX = startX / 8; // Byte head 끝 X 오프셋
        //                        headCount = startX & 7; // Byte head 의 총 바이트 수
        //                        headBufferOffset = bufferOffsetY + headBufferOffsetX; // Byte head 끝의 버퍼 오프셋
        //                        headArrayOffset = arrayOffsetY + startX - headCount; // Byte head 끝의 어레이 오프셋
        //                        for(ptr = headCount - 1; ptr >= 0; ptr--) // ptr : byte 단위
        //                        {
        //                            if(reachableBufferPtr[headBufferOffset + ptr] != 0) break; // 픽셀 데이터 검사
        //                            var vPixels = new Vector<byte>(grayArray, ptr);
        //                            Vector<byte> diff = Vector.Max(Vector..SubtractSaturate(vPixels, vTarget),
        //                                       Vector.SubtractSaturate(vTarget, vPixels));
        //                            for()
        //                                if(!isBrightnessValid(grayArray, targetBrightness, headArrayOffset, ptr)) break; // 밝기 검사
        //                            setBitNew(reachableBufferPtr, headBufferOffset, ptr);
        //                        }
        //                        ptr++; // 한 칸 돌아가기
        //                        break;
        //                }


        //            }



        //            for(left = targetX - 1; left >= 0; left--)
        //            {
        //                if(isBitSet(reachableBufferPtr, bufferOffsetY, left)) break; // 픽셀 데이터 검사
        //                if(!isBrightnessValid(grayArray, targetBrightness, arrayOffsetY, left)) break; // 밝기 검사
        //                setBit(reachableBufferPtr, bufferOffsetY, left); // 다 통과했을 경우 칠
        //            }
        //            left++;

        //            // 오른쪽으로 진행
        //            for(right = targetX + 1; right < imageWidth; right++)
        //            {
        //                if(isBitSet(reachableBufferPtr, bufferOffsetY, right)) break; // 픽셀 데이터 검사
        //                if(!isBrightnessValid(grayArray, targetBrightness, arrayOffsetY, right)) break; // 밝기 검사
        //                setBit(reachableBufferPtr, bufferOffsetY, right); // 다 통과했을 경우 칠
        //            }
        //            right--;

        //            // 윗줄 브랜치
        //            if((targetUpperY = targetY - 1) >= 0)
        //            {
        //                bufferOffsetUpperY = bufferOffsetY - bufferStride; // Y offset 계산
        //                arrayOffsetUpperY = arrayOffsetY - imageWidth;
        //                bool opened = false; // 처음은 닫힌 상태
        //                for(int curX = left; curX <= right; curX++)
        //                {
        //                    bool isOpenedBit = !isBitSet(reachableBufferPtr, bufferOffsetUpperY, curX) // 픽셀 데이터 검사
        //                        && isBrightnessValid(grayArray, targetBrightness, arrayOffsetUpperY, curX);
        //                    if(opened) { if(!isOpenedBit) opened = false; } // 열려 있다가 닫힐 때
        //                    else if(isOpenedBit)
        //                    {
        //                        setBit(reachableBufferPtr, bufferOffsetUpperY, curX);
        //                        markTargetStack.Push((curX, targetUpperY));
        //                        opened = true;
        //                    }

        //                }
        //            }

        //            // 아랫줄 브랜치
        //            if((targetLowerY = targetY + 1) < imageHeight)
        //            {
        //                bufferOffsetLowerY = bufferOffsetY + bufferStride; // Y offset 계산
        //                arrayOffsetLowerY = arrayOffsetY + imageWidth;
        //                bool opened = false; // 처음은 닫힌 상태
        //                for(int curX = left; curX <= right; curX++)
        //                {
        //                    bool isOpenedBit = !isBitSet(reachableBufferPtr, bufferOffsetLowerY, curX) // 픽셀 데이터 검사
        //                        && isBrightnessValid(grayArray, targetBrightness, arrayOffsetLowerY, curX);
        //                    if(opened) { if(!isOpenedBit) opened = false; } // 열려 있다가 닫힐 때
        //                    else if(isOpenedBit)
        //                    {
        //                        setBit(reachableBufferPtr, bufferOffsetLowerY, curX);
        //                        markTargetStack.Push((curX, targetLowerY));
        //                        opened = true;
        //                    }
        //                }
        //            }

        //            if(left < minX) minX = left;
        //            if(right > maxX) maxX = right;
        //            if(targetY < minY) minY = targetY;
        //            if(targetY > maxY) maxY = targetY;
        //        }
        //    }
        //    else // false (= 0) 로 칠할 때
        //    {
        //        while(markTargetStack.Count > 0)
        //        {
        //            var (targetX, targetY) = markTargetStack.Pop(); // 스택에서 대상을 하나 꺼내고

        //            bufferOffsetY = targetY * bufferStride;
        //            arrayOffsetY = targetY * imageWidth;

        //            // 왼쪽으로 진행
        //            for(left = targetX - 1; left >= 0; left--)
        //            {
        //                if(!isBitSet(reachableBufferPtr, bufferOffsetY, left)) break; // 픽셀 데이터 검사
        //                if(!isBrightnessValid(grayArray, targetBrightness, arrayOffsetY, left)) break; // 밝기 검사
        //                clearBit(reachableBufferPtr, bufferOffsetY, left); // 다 통과했을 경우 칠
        //            }
        //            left++;

        //            // 오른쪽으로 진행
        //            for(right = targetX + 1; right < imageWidth; right++)
        //            {
        //                if(!isBitSet(reachableBufferPtr, bufferOffsetY, right)) break; // 픽셀 데이터 검사
        //                if(!isBrightnessValid(grayArray, targetBrightness, arrayOffsetY, right)) break; // 밝기 검사
        //                clearBit(reachableBufferPtr, bufferOffsetY, right); // 다 통과했을 경우 칠
        //            }
        //            right--;

        //            // 윗줄 브랜치
        //            if((targetUpperY = targetY - 1) >= 0)
        //            {
        //                bufferOffsetUpperY = bufferOffsetY - bufferStride; // Y offset 계산
        //                arrayOffsetUpperY = arrayOffsetY - imageWidth;
        //                bool opened = false; // 처음은 닫힌 상태
        //                for(int curX = left; curX <= right; curX++)
        //                {
        //                    bool isOpenedBit = isBitSet(reachableBufferPtr, bufferOffsetUpperY, curX) // 픽셀 데이터 검사
        //                        && isBrightnessValid(grayArray, targetBrightness, arrayOffsetUpperY, curX);
        //                    if(opened) { if(!isOpenedBit) opened = false; } // 열려 있다가 닫힐 때
        //                    else if(isOpenedBit)
        //                    {
        //                        clearBit(reachableBufferPtr, bufferOffsetUpperY, curX);
        //                        markTargetStack.Push((curX, targetUpperY));
        //                        opened = true;
        //                    }

        //                }
        //            }

        //            // 아랫줄 브랜치
        //            if((targetLowerY = targetY + 1) < imageHeight)
        //            {
        //                bufferOffsetLowerY = bufferOffsetY + bufferStride; // Y offset 계산
        //                arrayOffsetLowerY = arrayOffsetY + imageWidth;
        //                bool opened = false; // 처음은 닫힌 상태
        //                for(int curX = left; curX <= right; curX++)
        //                {
        //                    bool isOpenedBit = isBitSet(reachableBufferPtr, bufferOffsetLowerY, curX) // 픽셀 데이터 검사
        //                        && isBrightnessValid(grayArray, targetBrightness, arrayOffsetLowerY, curX);
        //                    if(opened) { if(!isOpenedBit) opened = false; } // 열려 있다가 닫힐 때
        //                    else if(isOpenedBit)
        //                    {
        //                        clearBit(reachableBufferPtr, bufferOffsetLowerY, curX);
        //                        markTargetStack.Push((curX, targetLowerY));
        //                        opened = true;
        //                    }
        //                }
        //            }

        //            if(left < minX) minX = left;
        //            if(right > maxX) maxX = right;
        //            if(targetY < minY) minY = targetY;
        //            if(targetY > maxY) maxY = targetY;
        //        }
        //    }

        //    return new Int32Rect(minX, minY, maxX - minX, maxY - minY);


        //    // Gray Array 룩업 로컬 함수 (대상 픽셀 밝기 검사)
        //    static bool isBrightnessValid(byte[] lookupArray, byte stdBrightness, int offset, int i)
        //    {
        //        byte curBrightness = lookupArray[offset + i];
        //        return (curBrightness > BRTH_ABS) && (Math.Abs(stdBrightness - curBrightness) < BRTH_REL);
        //    }

        //    // 비트연산을 위한 로컬 함수 정의

        //    static bool isBitSetNew(byte* basePtr, int byteOffset, int i)
        //        => (basePtr[byteOffset] & ((byte)(1 << (7 - i)))) != 0;

        //    static void setBitNew(byte* basePtr, int byteOffset, int i)
        //        => basePtr[byteOffset] |= (byte)(1 << (7 - i));



        //    static bool isBitSet(byte* basePtr, int offsetY, int x)
        //        => (basePtr[offsetY + (x / 8)] & ((byte)(1 << (7 - x % 8)))) != 0;

        //    static void setBit(byte* basePtr, int offsetY, int x)
        //        => basePtr[offsetY + (x / 8)] |= (byte)(1 << (7 - x % 8));

        //    static void clearBit(byte* basePtr, int offsetY, int x)
        //        => basePtr[offsetY + (x / 8)] &= (byte)~(1 << (7 - x % 8));
        //}





        private static unsafe Int32Rect FloodFill(byte[] grayArray, byte* reachableBufferPtr,
            int imageWidth, int imageHeight, int bufferStride, bool value, (int X, int Y) targetInImage)
        {
            // 픽셀 범위 검사
            if(targetInImage.X < 0 || targetInImage.X >= imageWidth ||
                targetInImage.Y < 0 || targetInImage.Y >= imageHeight) return Int32Rect.Empty;

            // 픽셀 데이터 검사
            if(isBitSet(reachableBufferPtr, targetInImage.Y * bufferStride, targetInImage.X) == value) return Int32Rect.Empty;

            // Gray Array stride 계산
            int arrayStride = (imageWidth + 63) & ~63;

            // 출발점 픽셀 밝기 확인
            byte targetBrightness = grayArray[targetInImage.Y * arrayStride + targetInImage.X];
            if(targetBrightness < BRTH_ABS) return Int32Rect.Empty;

            // 해당 픽셀 칠하고 큐에 해당 픽셀 넣으며 알고리즘 시작
            if(value) setBit(reachableBufferPtr, targetInImage.Y * bufferStride, targetInImage.X);
            else clearBit(reachableBufferPtr, targetInImage.Y * bufferStride, targetInImage.X);
            var markTargetStack = new Stack<(int, int)>(512); // x, y
            markTargetStack.Push(targetInImage);

            // dirty rect
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            // 탐색 관련 변수
            int left, right;
            int targetUpperY, targetLowerY;
            int bufferOffsetY, bufferOffsetUpperY, bufferOffsetLowerY;
            int arrayOffsetY, arrayOffsetUpperY, arrayOffsetLowerY;

            // scanline floodfill
            if(value) // true (= 1) 로 칠할 때
            {
                while(markTargetStack.Count > 0)
                {
                    var (targetX, targetY) = markTargetStack.Pop(); // 스택에서 대상을 하나 꺼내고

                    bufferOffsetY = targetY * bufferStride;
                    arrayOffsetY = targetY * arrayStride;

                    // 왼쪽으로 진행
                    for(left = targetX - 1; left >= 0; left--)
                    {
                        if(isBitSet(reachableBufferPtr, bufferOffsetY, left)) break; // 픽셀 데이터 검사
                        if(!isBrightnessValid(grayArray, targetBrightness, arrayOffsetY, left)) break; // 밝기 검사
                        setBit(reachableBufferPtr, bufferOffsetY, left); // 다 통과했을 경우 칠
                    }
                    left++;

                    // 오른쪽으로 진행
                    for(right = targetX + 1; right < arrayStride; right++)
                    {
                        if(isBitSet(reachableBufferPtr, bufferOffsetY, right)) break; // 픽셀 데이터 검사
                        if(!isBrightnessValid(grayArray, targetBrightness, arrayOffsetY, right)) break; // 밝기 검사
                        setBit(reachableBufferPtr, bufferOffsetY, right); // 다 통과했을 경우 칠
                    }
                    right--;

                    // 윗줄 브랜치
                    if((targetUpperY = targetY - 1) >= 0)
                    {
                        bufferOffsetUpperY = bufferOffsetY - bufferStride; // Y offset 계산
                        arrayOffsetUpperY = arrayOffsetY - arrayStride;
                        bool opened = false; // 처음은 닫힌 상태
                        for(int curX = left; curX <= right; curX++)
                        {
                            bool isOpenedBit = !isBitSet(reachableBufferPtr, bufferOffsetUpperY, curX) // 픽셀 데이터 검사
                                && isBrightnessValid(grayArray, targetBrightness, arrayOffsetUpperY, curX);
                            if(opened) { if(!isOpenedBit) opened = false; } // 열려 있다가 닫힐 때
                            else if(isOpenedBit)
                            {
                                setBit(reachableBufferPtr, bufferOffsetUpperY, curX);
                                markTargetStack.Push((curX, targetUpperY));
                                opened = true;
                            }

                        }
                    }

                    // 아랫줄 브랜치
                    if((targetLowerY = targetY + 1) < imageHeight)
                    {
                        bufferOffsetLowerY = bufferOffsetY + bufferStride; // Y offset 계산
                        arrayOffsetLowerY = arrayOffsetY + arrayStride;
                        bool opened = false; // 처음은 닫힌 상태
                        for(int curX = left; curX <= right; curX++)
                        {
                            bool isOpenedBit = !isBitSet(reachableBufferPtr, bufferOffsetLowerY, curX) // 픽셀 데이터 검사
                                && isBrightnessValid(grayArray, targetBrightness, arrayOffsetLowerY, curX);
                            if(opened) { if(!isOpenedBit) opened = false; } // 열려 있다가 닫힐 때
                            else if(isOpenedBit)
                            {
                                setBit(reachableBufferPtr, bufferOffsetLowerY, curX);
                                markTargetStack.Push((curX, targetLowerY));
                                opened = true;
                            }
                        }
                    }

                    if(left < minX) minX = left;
                    if(right > maxX) maxX = right;
                    if(targetY < minY) minY = targetY;
                    if(targetY > maxY) maxY = targetY;
                }
            }
            else // false (= 0) 로 칠할 때
            {
                while(markTargetStack.Count > 0)
                {
                    var (targetX, targetY) = markTargetStack.Pop(); // 스택에서 대상을 하나 꺼내고

                    bufferOffsetY = targetY * bufferStride;
                    arrayOffsetY = targetY * arrayStride;

                    // 왼쪽으로 진행
                    for(left = targetX - 1; left >= 0; left--)
                    {
                        if(!isBitSet(reachableBufferPtr, bufferOffsetY, left)) break; // 픽셀 데이터 검사
                        if(!isBrightnessValid(grayArray, targetBrightness, arrayOffsetY, left)) break; // 밝기 검사
                        clearBit(reachableBufferPtr, bufferOffsetY, left); // 다 통과했을 경우 칠
                    }
                    left++;

                    // 오른쪽으로 진행
                    for(right = targetX + 1; right < arrayStride; right++)
                    {
                        if(!isBitSet(reachableBufferPtr, bufferOffsetY, right)) break; // 픽셀 데이터 검사
                        if(!isBrightnessValid(grayArray, targetBrightness, arrayOffsetY, right)) break; // 밝기 검사
                        clearBit(reachableBufferPtr, bufferOffsetY, right); // 다 통과했을 경우 칠
                    }
                    right--;

                    // 윗줄 브랜치
                    if((targetUpperY = targetY - 1) >= 0)
                    {
                        bufferOffsetUpperY = bufferOffsetY - bufferStride; // Y offset 계산
                        arrayOffsetUpperY = arrayOffsetY - arrayStride;
                        bool opened = false; // 처음은 닫힌 상태
                        for(int curX = left; curX <= right; curX++)
                        {
                            bool isOpenedBit = isBitSet(reachableBufferPtr, bufferOffsetUpperY, curX) // 픽셀 데이터 검사
                                && isBrightnessValid(grayArray, targetBrightness, arrayOffsetUpperY, curX);
                            if(opened) { if(!isOpenedBit) opened = false; } // 열려 있다가 닫힐 때
                            else if(isOpenedBit)
                            {
                                clearBit(reachableBufferPtr, bufferOffsetUpperY, curX);
                                markTargetStack.Push((curX, targetUpperY));
                                opened = true;
                            }

                        }
                    }

                    // 아랫줄 브랜치
                    if((targetLowerY = targetY + 1) < imageHeight)
                    {
                        bufferOffsetLowerY = bufferOffsetY + bufferStride; // Y offset 계산
                        arrayOffsetLowerY = arrayOffsetY + arrayStride;
                        bool opened = false; // 처음은 닫힌 상태
                        for(int curX = left; curX <= right; curX++)
                        {
                            bool isOpenedBit = isBitSet(reachableBufferPtr, bufferOffsetLowerY, curX) // 픽셀 데이터 검사
                                && isBrightnessValid(grayArray, targetBrightness, arrayOffsetLowerY, curX);
                            if(opened) { if(!isOpenedBit) opened = false; } // 열려 있다가 닫힐 때
                            else if(isOpenedBit)
                            {
                                clearBit(reachableBufferPtr, bufferOffsetLowerY, curX);
                                markTargetStack.Push((curX, targetLowerY));
                                opened = true;
                            }
                        }
                    }

                    if(left < minX) minX = left;
                    if(right > maxX) maxX = right;
                    if(targetY < minY) minY = targetY;
                    if(targetY > maxY) maxY = targetY;
                }
            }

            return new Int32Rect(minX, minY, maxX - minX, maxY - minY);


            // Gray Array 룩업 로컬 함수 (대상 픽셀 밝기 검사)
            static bool isBrightnessValid(byte[] lookupArray, byte stdBrightness, int offset, int i)
            {
                byte curBrightness = lookupArray[offset + i];
                return (curBrightness > BRTH_ABS) && (Math.Abs(stdBrightness - curBrightness) < BRTH_REL);
            }

            // 비트연산을 위한 로컬 함수 정의

            static bool isBitSet(byte* basePtr, int offsetY, int x)
                => (basePtr[offsetY + (x / 8)] & ((byte)(1 << (7 - x % 8)))) != 0;

            static void setBit(byte* basePtr, int offsetY, int x)
                => basePtr[offsetY + (x / 8)] |= (byte)(1 << (7 - x % 8));

            static void clearBit(byte* basePtr, int offsetY, int x)
                => basePtr[offsetY + (x / 8)] &= (byte)~(1 << (7 - x % 8));
        }

        //private static unsafe void FloodFill(byte[] grayArray, byte* reachableBufferPtr,
        //    int imageWidth, int imageHeight, int bufferStride, bool value, (int X, int Y) targetInImage)
        //{
        //    // 픽셀 범위 검사
        //    if(targetInImage.X < 0 || targetInImage.X >= imageWidth ||
        //        targetInImage.Y < 0 || targetInImage.Y >= imageHeight) return;

        //    // 픽셀 데이터 검사
        //    if(IsBitSet(reachableBufferPtr, bufferStride, targetInImage.X, targetInImage.Y) == value) return;

        //    // 출발점 픽셀 밝기
        //    byte targetBrightness = grayArray[targetInImage.Y * imageWidth + targetInImage.X];

        //    // 큐에 해당 픽셀 넣으며 알고리즘 시작
        //    var markTargetQueue = new Queue<(int, int)>(); // x, y
        //    markTargetQueue.Enqueue(targetInImage);

        //    // scanline floodfill
        //    if(value)
        //    {
        //        while(markTargetQueue.Count > 0)
        //        {
        //            var (targetX, targetY) = markTargetQueue.Dequeue();             // 큐에서 대상을 하나 꺼내고
        //            if(targetX < 0 || targetX >= imageWidth || targetY < 0 || targetY >= imageHeight) continue;  // 픽셀 범위 검사
        //            if(IsBitSet(reachableBufferPtr, bufferStride, targetX, targetY)) continue; // 픽셀 데이터 검사
        //            byte curBrightness = grayArray[targetY * imageWidth + targetX];
        //            if(curBrightness > BRTH_ABS && (Math.Abs(targetBrightness - curBrightness) < BRTH_REL))
        //            {
        //                SetBit(reachableBufferPtr, bufferStride, targetX, targetY); // 값 대입
        //                // 큐에 다음 대상 값 추가
        //                markTargetQueue.Enqueue((targetX - 1, targetY));
        //                markTargetQueue.Enqueue((targetX, targetY - 1));
        //                markTargetQueue.Enqueue((targetX + 1, targetY));
        //                markTargetQueue.Enqueue((targetX, targetY + 1));
        //            }
        //        }
        //    }
        //    else
        //    {
        //        while(markTargetQueue.Count > 0)
        //        {
        //            var (targetX, targetY) = markTargetQueue.Dequeue();             // 큐에서 대상을 하나 꺼내고
        //            if(targetX < 0 || targetX >= imageWidth || targetY < 0 || targetY >= imageHeight) continue;  // 픽셀 범위 검사
        //            if(!IsBitSet(reachableBufferPtr, bufferStride, targetX, targetY)) continue; // 픽셀 데이터 검사
        //            byte curBrightness = grayArray[targetY * imageWidth + targetX];

        //            if(curBrightness > BRTH_ABS && (Math.Abs(targetBrightness - curBrightness) < BRTH_REL))
        //            {
        //                ClearBit(reachableBufferPtr, bufferStride, targetX, targetY); // 값 대입
        //                // 큐에 다음 대상 값 추가
        //                markTargetQueue.Enqueue((targetX - 1, targetY));
        //                markTargetQueue.Enqueue((targetX, targetY - 1));
        //                markTargetQueue.Enqueue((targetX + 1, targetY));
        //                markTargetQueue.Enqueue((targetX, targetY + 1));
        //            }
        //        }
        //    }

        //    // 비트연산을 위한 로컬 함수 정의

        //    bool IsBitSet(byte* basePtr, int stride, int x, int y)
        //        => (basePtr[(y * stride) + (x / 8)] & ((byte)(1 << (7 - x % 8)))) != 0;

        //    void SetBit(byte* basePtr, int stride, int x, int y)
        //        => basePtr[(y * stride) + (x / 8)] |= (byte)(1 << (7 - x % 8));

        //    void ClearBit(byte* basePtr, int stride, int x, int y)
        //        => basePtr[(y * stride) + (x / 8)] &= (byte)~(1 << (7 - x % 8));
        //}

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
