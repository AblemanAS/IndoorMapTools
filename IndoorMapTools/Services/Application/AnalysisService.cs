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

using IndoorMapTools.Algorithm;
using IndoorMapTools.Algorithm.FGASolver;
using IndoorMapTools.Helper;
using IndoorMapTools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace IndoorMapTools.Services.Application
{
    public class AnalysisService
    {
        public static AnalysisResult AnalyzeReachability(Building building, IFGASolver fgaSolver,
            double reachableResolution, bool conservativeCellValidation, bool directedReachableCluster, Action<int> progressCb = null)
        {
            IntegerProgressBox progBox = null;

            // 랜드마크 리스트 초기화
            List<Landmark> landmarks = AnalysisAlgorithms.ExtractAllLandmarks(building.LandmarkGroups);

            /******************************** 1) OGM Generation ********************************/
            // OGM 생성 및 Segmentation
            if(progressCb != null) progBox = new((p) => progressCb.Invoke(p * 3 / 5)); // 진행률 0~60%
            Dictionary<Floor, List<System.Drawing.Bitmap>> ogmSegments = AnalysisAlgorithms.GenerateAndSegmentOGM(
                building.Floors, reachableResolution, conservativeCellValidation, progBox);

            // Segment 들을 FloorToAreas로 변환 (OGM segment 필요)
            if(progressCb != null) progBox = new((p) => progressCb.Invoke(60 + p / 20)); // 진행률 60~65%
            Dictionary<Floor, List<Area>> floorToAreas = AnalysisAlgorithms.OGMSegmentsToAreas(ogmSegments.ToArray(), progBox);

            // Areas 리스트 생성
            List<Area> areas = new();
            foreach(var kvp in floorToAreas) areas.AddRange(kvp.Value);

            // OGM 비트맵 메모리 해제
            foreach(var kvp in ogmSegments) foreach(var bmp in kvp.Value) bmp.Dispose();

            /********************** 2) Landmark - Reachable Area Coupling **********************/
            // 각 층별 Transform Matrix 캐시
            Dictionary<Floor, Matrix> transformers = new();
            foreach(var floor in building.Floors)
                floor.Reachable.Dispatcher.Invoke(() => transformers[floor] = CoordTransformAlgorithms.CalculateTransformer(
                    floor.Reachable.PixelWidth, floor.Reachable.PixelHeight, floor.MapImageRotation, 1 / floor.MapImagePPM));
            progBox?.Report(70); // 진행률 65~70%

            // Landmark - Area 커플링 (Areas, Transform Matrix 필요)
            Dictionary<Landmark, Area> landmarkToArea = AnalysisAlgorithms.CoupleLandmarksToAreas(
                floorToAreas.ToArray(), transformers, reachableResolution);
            progBox?.Report(75); // 진행률 70~75%

            /************************ 3) Reachability Graph Generation *************************/
            // Nodes 생성 (총 landmarkToArea 필요) => 실질적으로 GraphNode가 생성되는 단계
            Dictionary<Landmark, GraphNode> landmarkToNode = AnalysisAlgorithms.GenerateGraphNodes(landmarkToArea);
            progBox?.Report(80); // 진행률 75~80%

            // Edges 생성 (Areas, landmarkToNode 필요)
            AnalysisAlgorithms.BuildGraphEdges(areas, landmarkToNode, directedReachableCluster);
            progBox?.Report(85); // 진행률 80~85%

            /*************************** 4) Reachability Clustering ****************************/
            // Reachability Graph 클러스터링 (landmarks, areas, landmarkToNode 필요)
            List<GraphNode> reachableClusters = AnalysisAlgorithms.GetReachableClusters(landmarks, areas, landmarkToNode);
            progBox?.Report(90); // 진행률 85~90%

            /******************************* 5) Group Reordering *******************************/
            // FGA 행렬 구축 (locality를 위해서 FG가 아닌 G-row, F-col의 구조로 기록)
            // FGA Matrix는 Cell의 기본값이 Null이고, Area 값은 0으로 시작함
            // 이에 Null Cell과 Area 값 0 구분을 위해 Area 값에 +1 하여 대입
            // 따라서 값이 0인 경우는 Null Cell을 의미함
            var fgaMatrix = new int[building.LandmarkGroups.Count, building.Floors.Count]; // column, row
            foreach(GraphNode curNode in landmarkToNode.Values)
                fgaMatrix[curNode.Group, curNode.Floor] = curNode.Area + 1;
            int[] groupOrder = fgaSolver.Solve(fgaMatrix);
            progBox?.Report(95); // 진행률 90~95%

            // Isolated Area에 대해 사용 중이지 않은 Group Id incremental 부여 (고립 Area 로직)
            //int[] isolatedAreaGroupId = new int[building.Floors.Count];
            //for(int i = 0; i < isolatedAreaGroupId.Length; i++)
            //    isolatedAreaGroupId[i] = groupCount;
            //foreach(GraphNode curNode in reachableClusters)
            //    if(curNode.Data is Area curArea)
            //        curNode.ReassignGroup(isolatedAreaGroupId[curArea.FloorId]++);
            progBox?.Report(100); // 진행률 95~100%

            return new AnalysisResult(areas, landmarks, floorToAreas, landmarkToNode, reachableClusters, groupOrder);
        }
    }
}
