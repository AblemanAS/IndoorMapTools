using IndoorMapTools.Helper;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Bitmap = System.Drawing.Bitmap;

namespace IndoorMapTools.Core
{
    public class AnalysisModule
    {
        public static List<Bitmap> SegmentOGMtoImages(Bitmap ogm, double scale, Action<int> progressCb = null)
        {
            BitmapData scaledBitmapData = null;
            Bitmap curClusterBitmap = null;
            var result = new List<Bitmap>();
            var progBox = new IntegerProgressBox((p) => progressCb?.Invoke(p));

            // BitmapData 객체로 픽셀 데이터에 접근
            scaledBitmapData = ogm.LockBits(new System.Drawing.Rectangle(0, 0, ogm.Width, ogm.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format1bppIndexed);

            // 전체 이미지의 바이트 배열 생성
            int byteCount = scaledBitmapData.Stride * scaledBitmapData.Height;
            byte[] scaledBitmapArray = new byte[byteCount];
            IntPtr ptrFirstPixel = scaledBitmapData.Scan0;

            // 메모리에서 픽셀 데이터를 배열로 복사
            Marshal.Copy(ptrFirstPixel, scaledBitmapArray, 0, byteCount);
            progBox.Report(80);

            // 이미지 인스턴스 변환 파라미터 연산
            var transPalette = ogm.Palette;
            transPalette.Entries[0] = System.Drawing.Color.Transparent;
            transPalette.Entries[1] = System.Drawing.Color.FromArgb(200, 230, 230, 255);

            int totalPixelCount = ogm.Height * ogm.Width;
            int coveredPixelCount = 0;

            // 참조 array 생성
            var checkDone = new bool[ogm.Height, ogm.Width];

            // 픽셀 순회하며 클러스터링
            for(int y = 0; y < ogm.Height; y++)
            {
                for(int x = 0; x < ogm.Width; x++)
                {
                    if(checkDone[y, x]) continue; // 이미 체크한 픽셀이면 패스

                    // 이미 false인 픽셀은 체크 후 패스
                    if((scaledBitmapArray[y * scaledBitmapData.Stride + x / 8] & (0b10000000 >> (x % 8))) == 0)
                    {
                        checkDone[y, x] = true;
                        progBox.Report(++coveredPixelCount * 20 / totalPixelCount + 80);
                        continue;
                    }

                    // 새 클러스터 픽셀
                    byte[] curClusterArray = new byte[scaledBitmapData.Stride * scaledBitmapData.Height];

                    // 큐로 BFS 픽셀 서치
                    var pixelSearchQueue = new Queue<(int, int)>();
                    pixelSearchQueue.Enqueue((x, y));
                    while(pixelSearchQueue.Count > 0)
                    {
                        (int curX, int curY) = pixelSearchQueue.Dequeue();

                        // 범위 밖이거나 이미 체크한 픽셀이면 탈출
                        if(curX < 0 || curX >= ogm.Width || 
                            curY < 0 || curY >= ogm.Height || 
                            checkDone[curY, curX]) continue;

                        checkDone[curY, curX] = true;
                        progBox.Report(++coveredPixelCount * 20 / totalPixelCount + 80);

                        // 픽셀값 확인하여 색이 있을 경우 색칠하고 큐에 추가
                        int curByteIndex = curY * scaledBitmapData.Stride + curX / 8;
                        byte curMask = (byte)(0b10000000 >> (curX % 8));

                        if((scaledBitmapArray[curByteIndex] & curMask) == 0) continue;
                        curClusterArray[curByteIndex] |= curMask;

                        pixelSearchQueue.Enqueue((curX - 1, curY - 1));
                        pixelSearchQueue.Enqueue((curX - 1, curY));
                        pixelSearchQueue.Enqueue((curX - 1, curY + 1));
                        pixelSearchQueue.Enqueue((curX, curY - 1));
                        pixelSearchQueue.Enqueue((curX, curY + 1));
                        pixelSearchQueue.Enqueue((curX + 1, curY - 1));
                        pixelSearchQueue.Enqueue((curX + 1, curY));
                        pixelSearchQueue.Enqueue((curX + 1, curY + 1));
                    }

                    // 새 비트맵 생성 및 픽셀 데이터 복사
                    curClusterBitmap = new Bitmap(ogm.Width, ogm.Height, ogm.PixelFormat);
                    curClusterBitmap.SetResolution((float)(96 * scale), (float)(96 * scale));
                    curClusterBitmap.Palette = transPalette;
                    var curClusterBitmapData = curClusterBitmap.LockBits(
                        new System.Drawing.Rectangle(0, 0, curClusterBitmap.Width, curClusterBitmap.Height),
                        ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format1bppIndexed);
                    Marshal.Copy(curClusterArray, 0, curClusterBitmapData.Scan0, curClusterArray.Length);
                    curClusterBitmap.UnlockBits(curClusterBitmapData);
                    result.Add(curClusterBitmap);
                }
            }

            ogm?.UnlockBits(scaledBitmapData);

            progressCb?.Invoke(100);

            return result;
        }
    }
}
