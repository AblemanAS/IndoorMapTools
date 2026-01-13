//using CommunityToolkit.Mvvm.ComponentModel;
//using IndoorMapTools.Core;
//using IndoorMapTools.Helper;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Media;

//namespace IndoorMapTools.Model
//{
//    public partial class AnalysisReport : ObservableObject
//    {
//        private static readonly IFGASolver FGASolver = new TSPSolver();         // FGA Matrix Problem에 대한 Solver (기본값 : TSPSolver 사용)

//        public List<Landmark> Landmarks { get; } = new List<Landmark>();        // LMs
//        public List<GraphNode> GraphNodes { get; } = new List<GraphNode>();     // Nodes
//        public List<GraphNode> ReachableClusters { get; }                       // Node clusters
//        public Dictionary<Floor, List<Area>> ReachableAreas { get; }            // Area - Nodes
//        public Dictionary<Landmark, GraphNode> LandmarktoNode { get; }          // LM - Node
//        public List<Area> Areas { get; } = new List<Area>();

//        public int[] GroupOrder { get; }

//        public AnalysisReport(Building building, double reachableResolution, bool conservativeCellValidation,
//            bool isClusterDirected, Action<int> progressCb = null)
//        {
//            // 랜드마크 리스트 초기화
//            int entranceCount = 0;
//            foreach(var group in building.LandmarkGroups)
//            {
//                foreach(var lm in group.Landmarks)
//                {
//                    if(lm.ParentGroup.Type == LandmarkType.Entrance)
//                        Landmarks.Insert(entranceCount++, lm); // Entrance는 제일 앞으로
//                    else Landmarks.Add(lm); // Landmark 리스트에 추가
//                }
//            }

//            var progBox = new IntegerProgressBox((p) => progressCb?.Invoke(p));

//            // OGM 생성 및 ReachableAreas로 변환
//            var ogmSegmentsConcBag = new ConcurrentBag<(Floor, List<System.Drawing.Bitmap>)>();
//            var reachableAreaConcBag = new ConcurrentBag<(Floor, List<Area>)>();
//            Parallel.ForEach(building.Floors, curFloor =>
//            {
//                double reachableScale = 1.0 / reachableResolution / curFloor.MapImagePPM; // OGM 배율 계산
//                var ogm = ReachableAlgorithms.BuildOGMfromReachable(curFloor.Reachable, curFloor.MapImageRotation, // OGM 콜
//                    reachableScale, conservativeCellValidation, curFloor.Landmarks.Select(lm => lm.Outline));
//                var ogmSegments = AnalysisAlgorithms.SegmentOGMtoImages(ogm, reachableScale);
//                ogmSegmentsConcBag.Add((curFloor, ogmSegments)); // OGM Segments 저장
//                ogm.Dispose(); // OGM 해제

//                var ogmInImages = new List<Area>();

//                // 이미지 인스턴스 변환
//                for(int i = 0; i < ogmSegments.Count; i++)
//                {
//                    var curSegment = ogmSegments[i];
//                    curSegment.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY); // Y축 반전
//                    ogmInImages.Add(new Area(ImageAlgorithms.BitmapImageFromBitmap(curSegment), building.Floors.IndexOf(curFloor), i));
//                }

//                reachableAreaConcBag.Add((curFloor, ogmInImages)); // ReachableAreas로 변환
//            });
//            ReachableAreas = new Dictionary<Floor, List<Area>>(reachableAreaConcBag.Count);
//            foreach(var (floor, segmentedImages) in reachableAreaConcBag)
//                ReachableAreas.Add(floor, segmentedImages); // ReachableAreas에 추가
//            var ogmSegmentsDict = new Dictionary<Floor, List<System.Drawing.Bitmap>>(ogmSegmentsConcBag.Count);
//            foreach(var (floor, ogmSegments) in ogmSegmentsConcBag)
//                ogmSegmentsDict.Add(floor, ogmSegments); // ReachableAreas에 추가
//            progBox.Report(30);

//            // 각 층별 Transform Matrix 캐시
//            Dictionary<Floor, Matrix> transformers = new Dictionary<Floor, Matrix>();
//            foreach(var floor in building.Floors)
//                floor.Reachable.Dispatcher.Invoke(() =>
//                    transformers[floor] = MathAlgorithms.CalculateTransformer(floor.Reachable.PixelWidth,
//                    floor.Reachable.PixelHeight, floor.MapImageRotation, 1 / floor.MapImagePPM));
//            progBox.Report(40);

//            //// 각 층별 Landmark의 위치를 변환행렬을 통해 변환
//            //var landmarktoNodeConcBag = new ConcurrentBag<(Landmark, GraphNode)>();
//            //var nodesInAreaConcDict = new ConcurrentDictionary<Area, List<GraphNode>>();
//            //Parallel.For(0, building.Floors.Count, floorIndex =>
//            //{
//            //    byte[] buffer = new byte[1];
//            //    Floor curFloor = building.Floors[floorIndex]; // 현재 층
//            //    foreach(Landmark curLandmark in curFloor.Landmarks)
//            //    {
//            //        if(ogmSegmentsDict[curFloor].Count == 0) continue; // ReachableArea 없으면 패스

//            //        Point calculatedCenter = MathAlgorithms.CalculatePolygonCenter(curLandmark.Outline); // Center
//            //        Point transformedLoc = transformers[curFloor].Transform(calculatedCenter);       // Location
//            //        int targetCellX = (int)(transformedLoc.X / reachableResolution);
//            //        int targetCellY = (int)(ogmSegmentsDict[curFloor][0].Height - transformedLoc.Y / reachableResolution);

//            //        // 각 ReachableArea에 속하는지 판단 후 참조 설정
//            //        for(int areaIndex = 0; areaIndex < ReachableAreas[curFloor].Count; areaIndex++)
//            //        {
//            //            var curOgmSegment = ogmSegmentsDict[curFloor][areaIndex];

//            //            if(curOgmSegment.GetPixel(targetCellX, targetCellY).A != 0)
//            //            {
//            //                var curNode = new GraphNode(floorIndex, building.LandmarkGroups.IndexOf(curLandmark.ParentGroup),
//            //                    areaIndex, curLandmark);
//            //                landmarktoNodeConcBag.Add((curLandmark, curNode)); // Landmark -> Node 매핑
//            //                ReachableAreas[curFloor][areaIndex].Landmarks.Add(curNode);// Area에 Node 추가
//            //                break;
//            //            }
//            //        }
//            //    }
//            //    //foreach(var ogmInBitmap in ogmSegmentsDict[curFloor]) ogmInBitmap.Dispose();
//            //});
//            //LandmarktoNode = new Dictionary<Landmark, GraphNode>(landmarktoNodeConcBag.Count);
//            //foreach(var (landmark, node) in landmarktoNodeConcBag)
//            //    LandmarktoNode.Add(landmark, node); // Landmark -> Node 매핑
//            //foreach(Landmark curLandmark in Landmarks)
//            //    GraphNodes.Add(LandmarktoNode[curLandmark]); // GraphNodes에 추가
//            //progBox.Report(50);

//            //// 그래프 생성
//            //Areas = ReachableAreas.Values.SelectMany(areaList => areaList).ToList();
//            //Parallel.ForEach(Areas, curArea => // 각 층별 Area에 속하는 Node들
//            //{
//            //    foreach(Landmark curLandmark in curArea.Landmarks)
//            //    {
//            //        GraphNode curNode = LandmarktoNode[curLandmark]; // 현재 포인터 Node

//            //        // 현재 포인터의 랜드마크와 같은 Area에 속하는 Node들 중에서
//            //        foreach(GraphNode otherNode in curArea.Landmarks)
//            //        {
//            //            if(curNode == otherNode) continue; // 나 자신은 제외
//            //            curNode.Children.Add(otherNode); // 자식 추가
//            //        }

//            //        Landmark curLandmark = (Landmark)curNode.Data; // 현재 랜드마크

//            //        // 나를 통해 탈출만 가능한 경우 제외
//            //        if(isClusterDirected && curLandmark.AccessType == 1) continue;

//            //        // 같은 Group에 속하는 Landmark들 중에서
//            //        foreach(Landmark otherlandmark in curLandmark.ParentGroup.Landmarks)
//            //        {
//            //            if(curLandmark == otherlandmark) continue; // 나 자신은 제외
//            //            if(isClusterDirected && otherlandmark.AccessType == -1) continue; // 진입만 가능할 경우 제외
//            //            if(!LandmarktoNode.TryGetValue(otherlandmark, out var otherNode)) continue; // Node 없으면 패스
//            //            curNode.Children.Add(otherNode); // 자식 추가
//            //        }
//            //    }
//            //});
//            //progBox.Report(60);

//            //Console.WriteLine(Areas[0].Reachable.Format);

//            //// Reachability 클러스터링 (BFS, lower index wins)
//            //var reachableClustersConcBag = new ConcurrentBag<GraphNode>(); // 클러스터들
//            //Parallel.For(0, Landmarks.Count, i =>
//            //{
//            //    if(!LandmarktoNode.TryGetValue(Landmarks[i], out GraphNode rootNode)) return; // Node 없으면 패스

//            //    int rootNodeIndex = i;
//            //    bool isLoser = false;
//            //    var checkedNodes = new HashSet<GraphNode> { rootNode }; // 체크된 노드들
//            //    var nodeScanQueue = new Queue<(GraphNode, bool)>(); // 큐 초기화
//            //    nodeScanQueue.Enqueue((rootNode, true)); // BFS 시작

//            //    while(!isLoser && nodeScanQueue.Count > 0)
//            //    {
//            //        (GraphNode curNode, bool isTwoWay) = nodeScanQueue.Dequeue(); // 큐에서 꺼내기

//            //        // 자식 노드들 중에서
//            //        foreach(GraphNode childNode in curNode.Children)
//            //        {
//            //            if(checkedNodes.Contains(childNode)) continue; // 이미 체크된 노드면 패스
//            //            checkedNodes.Add(childNode); // 체크된 노드에 추가

//            //            // 무방향 설정이거나, 현재 서브그래프가 쌍방이면서 현재 자식노드가 쌍방일 경우
//            //            if(!isClusterDirected || isTwoWay && childNode.Children.Contains(curNode))
//            //            {
//            //                if(Landmarks.IndexOf((Landmark)childNode.Data) < rootNodeIndex) // 나보다 인덱스가 낮으면
//            //                { isLoser = true; break; } // 패배
//            //                else nodeScanQueue.Enqueue((childNode, true)); // 아니면 큐에 쌍방 서브그래프로 추가
//            //            }
//            //            else nodeScanQueue.Enqueue((childNode, false)); // 이 중 하나라도 아니면 큐에 일방 서브그래프로 추가
//            //        }
//            //    }

//            //    // 최종 승리 시 클러스터로 추가
//            //    if(!isLoser) reachableClustersConcBag.Add(rootNode);
//            //});
//            //ReachableClusters = new List<GraphNode>(reachableClustersConcBag.Count);
//            //foreach(var curNode in reachableClustersConcBag)
//            //    ReachableClusters.Add(curNode); // 클러스터들에 추가

//            //// Landmark 연결되지 않은 ReachableArea는 따로 GraphNode 클러스터로 추가
//            //foreach(var kvPair in ReachableAreas)
//            //{
//            //    Floor curFloor = kvPair.Key; // 현재 층
//            //    List<Area> curAreas = kvPair.Value; // 현재 층의 ReachableArea들
//            //    for(int areaIndex = 0; areaIndex < curAreas.Count; areaIndex++)
//            //    {
//            //        Area curArea = curAreas[areaIndex]; // 현재 ReachableArea
//            //        if(curArea.Landmarks.Count == 0)
//            //            ReachableClusters.Add(new GraphNode(building.Floors.IndexOf(curFloor), -1, areaIndex, curArea));
//            //    }
//            //}
//            //progBox.Report(80);

//            //// FGA 행렬 구축 (locality를 위해서 FG가 아닌 G-row, F-col의 구조로 기록)
//            //var fgaMatrix = new int[building.LandmarkGroups.Count, building.Floors.Count]; // column, row
//            //foreach(var kvPair in ReachableAreas)
//            //    foreach(var curArea in kvPair.Value)
//            //        foreach(var curNode in curArea.Landmarks)
//            //            // FGA Matrix는 Cell의 기본값이 Null이고, Area 값은 0으로 시작함
//            //            // 이에 Null Cell과 Area 값 0 구분을 위해 Area 값에 +1 하여 대입
//            //            // 따라서 값이 0인 경우는 Null Cell을 의미함
//            //            fgaMatrix[curNode.Group, curNode.Floor] = curNode.Area + 1;

//            //GroupOrder = FGASolver.Solve(fgaMatrix);
//        }
//    }
//}
