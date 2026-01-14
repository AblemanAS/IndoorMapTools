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

using IndoorMapTools.Helper;
using IndoorMapTools.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bitmap = System.Drawing.Bitmap;

namespace IndoorMapTools.Core
{
    public class AnalysisAlgorithms
    {
        public static List<Landmark> ExtractAllLandmarks(IReadOnlyList<LandmarkGroup> groups)
        {
            List<Landmark> landmarks = new();
            foreach(var group in groups)
                if(group.Type == LandmarkType.Entrance)
                    foreach(var lm in group.Landmarks)
                        landmarks.Add(lm); // Entrance 먼저 리스트 추가
            foreach(var group in groups)
                if(group.Type != LandmarkType.Entrance)
                    foreach(var lm in group.Landmarks)
                        landmarks.Add(lm); // 나머지 유형은 나중에 추가
            return landmarks;
        }

        // OGM 생성
        public static Dictionary<Floor, List<System.Drawing.Bitmap>> GenerateAndSegmentOGM(IReadOnlyList<Floor> floors,
            double reachableResolution, bool conservativeCellValidation, IProgress<int> progressCb = null)
        {
            var ogmSegmentsArray = new List<System.Drawing.Bitmap>[floors.Count];

            // 누적식 진행률 박스 생성
            CumulativeProgressBox cpb = null;
            if(progressCb != null) cpb = new CumulativeProgressBox(floors.Count * 2 + 1, (p) => progressCb.Report(p));

            // 각 층
            Parallel.For(0, floors.Count, floorIndex =>
            {
                Bitmap ogm = null;
                var curFloor = floors[floorIndex];

                try
                {
                    double reachableScale = 1.0 / reachableResolution / curFloor.MapImagePPM; // OGM 배율 계산

                    // OGM Generation
                    ogm = ReachableAlgorithms.BuildOGMfromReachable(curFloor.Reachable, curFloor.MapImageRotation,
                        reachableScale, conservativeCellValidation, curFloor.Landmarks.Select(lm => lm.Outline));
                    cpb?.Report(1); // 진행률 보고

                    // OGM Segmentation
                    var ogmSegments = SegmentOGMtoImages(ogm, reachableScale);
                    ogmSegmentsArray[floorIndex] = ogmSegments; // OGM Segments 저장
                    cpb?.Report(1); // 진행률 보고
                }
                finally { ogm?.Dispose(); } // OGM 해제
            });

            // OGM Segments Dictionary 생성
            var ogmSegmentsDict = new Dictionary<Floor, List<Bitmap>>(ogmSegmentsArray.Length);
            for(int floorIndex = 0; floorIndex < floors.Count; floorIndex++)
                ogmSegmentsDict.Add(floors[floorIndex], ogmSegmentsArray[floorIndex]); // OGM Segments 에 추가
            cpb?.Report(1); // 진행률 보고

            return ogmSegmentsDict;
        }


        // private으로 전환해야 함
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
                    var pixelSearchStack = new Stack<(int, int)>();
                    pixelSearchStack.Push((x, y));
                    while(pixelSearchStack.Count > 0)
                    {
                        (int curX, int curY) = pixelSearchStack.Pop();

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

                        pixelSearchStack.Push((curX - 1, curY - 1));
                        pixelSearchStack.Push((curX - 1, curY));
                        pixelSearchStack.Push((curX - 1, curY + 1));
                        pixelSearchStack.Push((curX, curY - 1));
                        pixelSearchStack.Push((curX, curY + 1));
                        pixelSearchStack.Push((curX + 1, curY - 1));
                        pixelSearchStack.Push((curX + 1, curY));
                        pixelSearchStack.Push((curX + 1, curY + 1));
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


        // OGM 생성 및 Areas로 변환
        public static Dictionary<Floor, List<Area>> OGMSegmentsToAreas(
            IReadOnlyList<KeyValuePair<Floor, List<System.Drawing.Bitmap>>> ogmSegments, IProgress<int> progressCb = null)
        {
            var areaArray = new List<Area>[ogmSegments.Count];

            int floorCount = ogmSegments.Count;

            // 누적식 진행률 박스 생성
            CumulativeProgressBox cpb = null;
            if(progressCb != null) cpb = new CumulativeProgressBox(floorCount + 1, (p) => progressCb.Report(p));

            // 각 층
            Parallel.For(0, ogmSegments.Count, floorIndex =>
            {
                // 이미지 인스턴스 변환
                var kvpair = ogmSegments[floorIndex];
                Floor curFloor = kvpair.Key;
                List<System.Drawing.Bitmap> curFloorSegments = kvpair.Value;
                List<Area> ogmInImages = new();
                for(int i = 0; i < curFloorSegments.Count; i++)
                {
                    var curSegment = curFloorSegments[i];
                    curSegment.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY); // Y축 반전
                    ogmInImages.Add(new Area(ImageAlgorithms.BitmapImageFromBitmap(curSegment), floorIndex, i));
                }
                areaArray[floorIndex] = ogmInImages; // ReachableAreas로 변환
                cpb?.Report(1); // 진행률 보고
            });

            // Areas Dictionary 생성
            var areas = new Dictionary<Floor, List<Area>>(areaArray.Length);
            for(int floorIndex = 0; floorIndex < areaArray.Length; floorIndex++)
                areas.Add(ogmSegments[floorIndex].Key, areaArray[floorIndex]); // Areas 에 추가
            cpb?.Report(1); // 진행률 보고
            return areas;
        }


        // 각 층별 Landmark의 위치를 변환행렬을 통해 변환
        public static Dictionary<Landmark, Area> CoupleLandmarksToAreas(IReadOnlyList<KeyValuePair<Floor, List<Area>>> areas,
            IReadOnlyDictionary<Floor, Matrix> transformers, double reachableResolution)
        {
            if(areas.Count == 0) return null;

            // 각 층별 랜드마크 수 및 Landmark Area kvpair array 생성 
            int floorCount = areas.Count;
            int totalLandmarkCount = 0;
            var landmarkToAreaKVParr = new (Landmark, Area)[floorCount][];
            for(int i = 0; i < floorCount; i++)
            {
                int landmarkCount = areas[i].Key.Landmarks.Count;
                landmarkToAreaKVParr[i] = new (Landmark, Area)[landmarkCount];
                totalLandmarkCount += landmarkCount;
            }

            Parallel.For(0, floorCount, floorIndex =>
            {
                Floor curFloor = areas[floorIndex].Key; // 현재 층
                List<Area> curAreas = areas[floorIndex].Value; // 현재 층의 area들
                if(curAreas.Count == 0) return; // ReachableArea 없으면 패스

                // 커플링 관련 변수
                Matrix transformer = transformers[curFloor]; // 현재 층의 변환 행렬 (랜드마크 좌표 -> 미터좌표)
                byte[] pixelBuffer = new byte[1];
                var sourceRect = new Int32Rect(0, 0, 1, 1);
                int reachableHeight = curAreas[0].Reachable.PixelHeight;

                // 각 Landmark 순회
                for(int landmarkIndex = 0; landmarkIndex < curFloor.Landmarks.Count; landmarkIndex++)
                {
                    Landmark curLandmark = curFloor.Landmarks[landmarkIndex];
                    Point calculatedCenter = MathAlgorithms.CalculatePolygonCenter(curLandmark.Outline); // Center
                    Point transformedLoc = transformer.Transform(calculatedCenter); // Location
                    sourceRect.X = (int)(transformedLoc.X / reachableResolution);
                    sourceRect.Y = (int)(reachableHeight - transformedLoc.Y / reachableResolution);

                    // 각 ReachableArea에 속하는지 판단 후 참조 설정
                    foreach(Area curArea in curAreas)
                    {
                        BitmapImage curReachableSegment = curArea.Reachable;
                        curReachableSegment.CopyPixels(sourceRect, pixelBuffer, curReachableSegment.PixelWidth, 0);

                        if((pixelBuffer[0] & 0x80) != 0)
                        {
                            landmarkToAreaKVParr[floorIndex][landmarkIndex] = (curLandmark, curArea);
                            curArea.Landmarks.Add(curLandmark); // Area에 Landmark 추가
                            break;
                        }
                    }
                }
            });

            var LandmarktoArea = new Dictionary<Landmark, Area>(totalLandmarkCount);
            foreach(var e in landmarkToAreaKVParr)
                foreach((Landmark key, Area value) in e)
                    LandmarktoArea.Add(key, value); // Landmark -> Area 매핑

            return LandmarktoArea;
        }


        // GraphNode를 생성하고 Landmark to GraphNode Dictionary로 반환
        public static Dictionary<Landmark, GraphNode> GenerateGraphNodes(IReadOnlyDictionary<Landmark, Area> landmarkToArea)
        {
            int landmarkCount = landmarkToArea.Count;
            if(landmarkCount == 0) return null;

            // 랜드마크 순회용 열거자 생성 및 빌딩, 층, 그룹 참조 변수 생성
            var landmarks = landmarkToArea.Keys;
            using var enumerator = landmarks.GetEnumerator();
            enumerator.MoveNext();
            var building = enumerator.Current.ParentFloor.ParentBuilding;
            var floors = building.Floors;
            var groups = building.LandmarkGroups;

            var landmarkToNode = new Dictionary<Landmark, GraphNode>(landmarkCount);

            foreach(Landmark curLandmark in landmarks)
            {
                Area curArea = landmarkToArea[curLandmark];
                int floorIndex = floors.IndexOf(curLandmark.ParentFloor);
                int groupIndex = groups.IndexOf(curLandmark.ParentGroup);
                int areaIndex = curArea.AreaId;
                var curNode = new GraphNode(floorIndex, groupIndex, areaIndex, curLandmark);
                landmarkToNode.Add(curLandmark, curNode);
            }

            return landmarkToNode;
        }


        // GraphNode 간의 Edge 생성
        public static void BuildGraphEdges(IReadOnlyList<Area> areas,
            IReadOnlyDictionary<Landmark, GraphNode> landmarkToNode, bool isClusterDirected)
        {
            Parallel.ForEach(areas, curArea => // 각 층별 Area에 속하는 Node들
            {
                foreach(Landmark curLandmark in curArea.Landmarks)
                {
                    GraphNode curNode = landmarkToNode[curLandmark]; // 현재 포인터 Node

                    // 현재 포인터의 랜드마크와 같은 Area에 속하는 Node들 중에서
                    foreach(Landmark otherLandmark in curArea.Landmarks)
                    {
                        GraphNode otherNode = landmarkToNode[otherLandmark];
                        if(curNode == otherNode) continue; // 나 자신은 제외
                        curNode.Children.Add(otherNode); // 자식 추가
                    }

                    // 나를 통해 탈출만 가능한 경우 제외
                    if(isClusterDirected && curLandmark.AccessType == 1) continue;

                    // 같은 Group에 속하는 Landmark들 중에서
                    foreach(Landmark otherlandmark in curLandmark.ParentGroup.Landmarks)
                    {
                        if(curLandmark == otherlandmark) continue; // 나 자신은 제외
                        if(isClusterDirected && otherlandmark.AccessType == -1) continue; // 진입만 가능할 경우 제외
                        if(!landmarkToNode.TryGetValue(otherlandmark, out var otherNode)) continue; // Node 없으면 패스
                        curNode.Children.Add(otherNode); // 자식 추가
                    }
                }
            });
        }


        public static List<GraphNode> GetReachableClusters(IReadOnlyList<Landmark> landmarks,
            IReadOnlyList<Area> areas, IReadOnlyDictionary<Landmark, GraphNode> landmarkToNode)
        {
            // Reachability 클러스터링 (BFS, lower index wins)
            var reachableClustersConcBag = new ConcurrentBag<GraphNode>(); // 클러스터들

            Parallel.For(0, landmarks.Count, i =>
            {
                if(!landmarkToNode.TryGetValue(landmarks[i], out GraphNode rootNode)) return; // Node 없으면 패스

                int rootNodeIndex = i;
                bool isLoser = false;
                var checkedNodes = new HashSet<GraphNode> { rootNode }; // 체크된 노드들
                var nodeScanQueue = new Queue<(GraphNode, bool)>(); // 큐 초기화
                nodeScanQueue.Enqueue((rootNode, true)); // BFS 시작

                while(!isLoser && nodeScanQueue.Count > 0)
                {
                    (GraphNode curNode, bool isTwoWay) = nodeScanQueue.Dequeue(); // 큐에서 꺼내기

                    // 자식 노드들 중에서
                    foreach(GraphNode childNode in curNode.Children)
                    {
                        if(checkedNodes.Contains(childNode)) continue; // 이미 체크된 노드면 패스
                        checkedNodes.Add(childNode); // 체크된 노드에 추가

                        // 무방향 설정이거나, 현재 서브그래프가 쌍방이면서 현재 자식노드가 쌍방일 경우
                        if(isTwoWay && childNode.Children.Contains(curNode))
                        {
                            if(landmarks.IndexOf((Landmark)childNode.Data) < rootNodeIndex) // 나보다 인덱스가 낮으면
                            { isLoser = true; break; } // 패배
                            else nodeScanQueue.Enqueue((childNode, true)); // 아니면 큐에 쌍방 서브그래프로 추가
                        }
                        else nodeScanQueue.Enqueue((childNode, false)); // 이 중 하나라도 아니면 큐에 일방 서브그래프로 추가
                    }
                }

                // 최종 승리 시 클러스터로 추가
                if(!isLoser) reachableClustersConcBag.Add(rootNode);
            });

            var reachableClusters = new List<GraphNode>(reachableClustersConcBag.Count);
            foreach(var curNode in reachableClustersConcBag) reachableClusters.Add(curNode); // 클러스터들에 추가

            // Landmark 연결되지 않은 ReachableArea는 따로 GraphNode 클러스터로 추가
            foreach(Area curArea in areas)
                if(curArea.Landmarks.Count == 0)
                    reachableClusters.Add(new GraphNode(curArea.FloorId, -1, curArea.AreaId, curArea));

            return reachableClusters;
        }
    }
}
