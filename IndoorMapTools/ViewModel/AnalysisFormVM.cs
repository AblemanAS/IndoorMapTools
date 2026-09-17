/********************************************************************************
Copyright 2026-present Korea Advanced Institute of Science and Technology (KAIST)

Author: Kyuho Son

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
********************************************************************************/

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Algorithm;
using IndoorMapTools.Algorithm.FGASolver;
using IndoorMapTools.Model;
using IndoorMapTools.Services.Application;
using IndoorMapTools.Services.Domain;
using IndoorMapTools.Services.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace IndoorMapTools.ViewModel
{
    public partial class AnalysisFormVM : ObservableObject
    {
        // 서비스
        private readonly BackgroundService bgSvc;
        private readonly IResourceStringService strSvc;

        // 솔버 정의
        private readonly IFGASolver fgaSolver = new TSPSolver();

        // 데이터 영역
        [ObservableProperty] private Project model;
        [ObservableProperty] private AnalysisResult result;

        [ObservableProperty] private Landmark selectedFGAViewLandmarkItem;
        [ObservableProperty] private Landmark selectedMapViewLandmarkItem;
        [ObservableProperty] private Area selectedFGAViewAreaItem;
        [ObservableProperty] private Area selectedMapViewAreaItem;
        [ObservableProperty] private Floor selectedFloor;
        [ObservableProperty] private GraphNode selectedCluster;

        [ObservableProperty] private IReadOnlyList<FGAEdgeData> intraGroupEdges;
        [ObservableProperty] private IReadOnlyDictionary<GraphNode, IReadOnlyList<FGAEdgeData>> graphEdges;
        [ObservableProperty] private Point mapViewFocus;
        [ObservableProperty] private string selectedItemSummary;
        public Dictionary<Floor, double[]> AreaPivots { get; } = new();
        public Dictionary<Area, Point> AreaPseudoCenter { get; } = new();
        
        [NotifyCanExecuteChangedFor(nameof(AnalyzeReachabilityCommand))]
        [ObservableProperty] private bool areLandmarkOutlinesComplete;

        private bool guardSelectPropagation = false;
        private object analysisGeneration = new();


        public AnalysisFormVM(BackgroundService bgSvc, IResourceStringService strSvc)
        {
            this.bgSvc = bgSvc;
            this.strSvc = strSvc;
        }


        partial void OnSelectedFGAViewLandmarkItemChanged(Landmark value)
        {
            if(guardSelectPropagation) return;
            guardSelectPropagation = true;
            try
            {
                if(SelectedMapViewLandmarkItem != value)
                    SelectedMapViewLandmarkItem = value;
                SelectedFGAViewAreaItem = null;
                SelectedMapViewAreaItem = null;
                SelectedCluster = null;
            }
            finally { guardSelectPropagation = false; }

            SelectedItemSummary = value?.ToString();
            Floor newFloor = value?.ParentFloor;
            if(SelectedFloor != newFloor) SelectedFloor = newFloor;
            if(value != null) MapViewFocus = CoordTransformAlgorithms.CalculatePolygonCenter(value.Outline);
        }


        partial void OnSelectedFGAViewAreaItemChanged(Area value)
        {
            if(guardSelectPropagation) return;
            guardSelectPropagation = true;
            try
            {
                SelectedFGAViewLandmarkItem = null;
                SelectedMapViewLandmarkItem = null;
                if(SelectedMapViewAreaItem != value)
                    SelectedMapViewAreaItem = value;
                SelectedCluster = null;
            }
            finally { guardSelectPropagation = false; }

            SelectedItemSummary = value?.ToString();
            LookFloorAndFocusArea(value);
        }


        partial void OnSelectedMapViewLandmarkItemChanged(Landmark value)
        {
            if(guardSelectPropagation) return;
            guardSelectPropagation = true;
            try
            {
                if(SelectedFGAViewLandmarkItem != value)
                    SelectedFGAViewLandmarkItem = value;
                SelectedFGAViewAreaItem = null;
                SelectedMapViewAreaItem = null;
                SelectedCluster = null;
            }
            finally { guardSelectPropagation = false; }

            SelectedItemSummary = value?.ToString();
            Floor newFloor = value?.ParentFloor;
            if(SelectedFloor != newFloor) SelectedFloor = newFloor;
        }


        partial void OnSelectedMapViewAreaItemChanged(Area value)
        {
            if(guardSelectPropagation) return;
            guardSelectPropagation = true;
            try
            {
                SelectedFGAViewLandmarkItem = null;
                SelectedMapViewLandmarkItem = null;
                if(SelectedFGAViewAreaItem != value)
                    SelectedFGAViewAreaItem = value;
                SelectedCluster = null;
            }
            finally { guardSelectPropagation = false; }

            SelectedItemSummary = value?.ToString();
        }


        partial void OnSelectedClusterChanged(GraphNode value)
        {
            if(guardSelectPropagation) return;
            guardSelectPropagation = true;
            try
            {
                SelectedFGAViewLandmarkItem = null;
                SelectedMapViewLandmarkItem = null;
                SelectedFGAViewAreaItem = null;
                if(value != null && value.Data is Area isolatedArea)
                {
                    SelectedMapViewAreaItem = isolatedArea;
                    LookFloorAndFocusArea(isolatedArea);
                }
                else SelectedMapViewAreaItem = null;
            }
            finally { guardSelectPropagation = false; }

            SelectedItemSummary = (value?.Data as Area)?.ToString();
        }


        private void LookFloorAndFocusArea(Area area)
        {
            if(area == null)
            {
                SelectedFloor = null;
                return;
            }

            Floor newFloor = Model.Building.Floors[area.FloorId];
            if(SelectedFloor != newFloor) SelectedFloor = newFloor;

            if(area.Landmarks.Count != 0)
                MapViewFocus = CoordTransformAlgorithms.CalculatePolygonCenter(area.Landmarks[0].Outline);
            else MapViewFocus = AreaPseudoCenter[area];
        }


        [RelayCommand] private void ValidateLandmarkCompleteness()
            => AreLandmarkOutlinesComplete = EntityValidator.AreLandmarkOutlinesComplete(Model.Building.LandmarkGroups);

        [RelayCommand] private void AnalyzeReachability()
        {
            if(bgSvc.IsBusy || Model == null) return;

            ClearAnalysisResult();
            var generation = analysisGeneration;
            var project = Model;
            var building = project.Building;
            double resolution = project.ReachableResolution;
            bool conservative = project.ConservativeCellValidation;
            bool directed = project.DirectedReachableCluster;
            AnalysisResult pendingResult = null;
            IReadOnlyList<FGAEdgeData> pendingIntraGroupEdges = null;
            IReadOnlyDictionary<GraphNode, IReadOnlyList<FGAEdgeData>> pendingGraphEdges = null;
            Dictionary<Floor, double[]> pendingAreaPivots = null;
            Dictionary<Area, Point> pendingAreaPseudoCenter = null;

            bgSvc.Run(() =>
            {
                var result = AnalysisService.AnalyzeReachability(building, fgaSolver, resolution,
                    conservative, directed, bgSvc.ReportProgress);
                var intraGroupEdges = BuildIntraGroupEdges(result, building);
                var graphEdges = BuildGraphEdges(result.ReachableClusters);

                var areaPivots = new Dictionary<Floor, double[]>();
                var areaPseudoCenter = new Dictionary<Area, Point>();

                for(int i = 0; i < building.Floors.Count; i++)
                {
                    var floor = building.Floors[i];
                    var curFloorAreas = result.FloorToAreas[floor];
                    if(curFloorAreas.Count == 0)
                    {
                        areaPivots[floor] = new double[] { 0.0, 0.0, 0.0, 0.0 };
                        continue;
                    }

                    int imageWidth = floor.MapImage.PixelWidth;
                    int imageHeight = floor.MapImage.PixelHeight;
                    var transformer = CoordTransformAlgorithms.CalculateTransformer(
                        imageWidth, imageHeight, floor.MapImageRotation, 1.0);
                    double newHeight = curFloorAreas[0].Reachable.PixelHeight * floor.MapImagePPM * resolution;
                    var pivot = new Point(0, imageHeight);
                    Point moved = transformer.Transform(pivot);
                    areaPivots[floor] = new double[] { moved.X, newHeight - moved.Y, -moved.X, moved.Y };
                }

                foreach(GraphNode curNode in result.ReachableClusters)
                {
                    if(curNode.Data is Area isolatedArea)
                    {
                        var floor = building.Floors[isolatedArea.FloorId];
                        var center = ReachableAlgorithms.GetPseudoReachableCenter(isolatedArea.Reachable);
                        var transformer = CoordTransformAlgorithms.CalculateTransformer(
                            floor.MapImage.PixelWidth, floor.MapImage.PixelHeight,
                            floor.MapImageRotation, 1.0 / floor.MapImagePPM / resolution);
                        transformer.Invert();
                        // Undo the area bitmap's vertical flip before returning to map-image pixels.
                        center.Y = isolatedArea.Reachable.PixelHeight - center.Y;
                        areaPseudoCenter[isolatedArea] = transformer.Transform(center);
                    }
                }

                pendingResult = result;
                pendingIntraGroupEdges = intraGroupEdges;
                pendingGraphEdges = graphEdges;
                pendingAreaPivots = areaPivots;
                pendingAreaPseudoCenter = areaPseudoCenter;
            }, () =>
            {
                try
                {
                    // Closing the dialog invalidates this run before its UI publication.
                    if(!ReferenceEquals(generation, analysisGeneration) || !ReferenceEquals(project, Model)) return;

                    foreach(var item in pendingAreaPivots) AreaPivots.Add(item.Key, item.Value);
                    foreach(var item in pendingAreaPseudoCenter) AreaPseudoCenter.Add(item.Key, item.Value);
                    IntraGroupEdges = pendingIntraGroupEdges;
                    GraphEdges = pendingGraphEdges;
                    Result = pendingResult;
                }
                finally
                {
                    // BackgroundService retains its delegates until the next Run.
                    pendingResult = null;
                    pendingIntraGroupEdges = null;
                    pendingGraphEdges = null;
                    pendingAreaPivots = null;
                    pendingAreaPseudoCenter = null;
                }
            }, strSvc["ReachableClusterAnalysisStatusDesc"]);
        }


        private static List<FGAEdgeData> BuildIntraGroupEdges(AnalysisResult result, Building building)
        {
            List<FGAEdgeData> edges = new();
            foreach(LandmarkGroup group in building.LandmarkGroups)
            {
                var sortedLandmarks = group.Landmarks.OrderBy(lm => building.Floors.IndexOf(lm.ParentFloor)).ToList();
                for(int i = 1; i < sortedLandmarks.Count; i++)
                {
                    var startNode = result.LandmarkToNode[sortedLandmarks[i - 1]];
                    var endNode = result.LandmarkToNode[sortedLandmarks[i]];
                    edges.Add(new FGAEdgeData(new FGAData(startNode.Floor, startNode.Group, startNode.Area), 
                        new FGAData(endNode.Floor, endNode.Group, endNode.Area)));
                }
            }

            //var lowestDict = new Dictionary<LandmarkGroup, Landmark>();
            //var highestDict = new Dictionary<LandmarkGroup, Landmark>();
            //for(int i = 0; i < building.Floors.Count; i++)
            //    foreach(Landmark curLM in building.Floors[i].Landmarks)
            //        if(!lowestDict.ContainsKey(curLM.ParentGroup)) lowestDict[curLM.ParentGroup] = curLM;
            //for(int i = building.Floors.Count - 1; i >= 0; i--)
            //    foreach(Landmark curLM in building.Floors[i].Landmarks)
            //        if(!highestDict.ContainsKey(curLM.ParentGroup)) highestDict[curLM.ParentGroup] = curLM;

            //foreach(var group in lowestDict.Keys)
            //{
            //    var lowestLM = lowestDict[group];
            //    var highestLM = highestDict[group];
            //    if(lowestLM == highestLM) continue;
            //    var startNode = result.LandmarkToNode[lowestLM];
            //    var endNode = result.LandmarkToNode[highestLM];
            //    edges.Add(new FGAEdgeData(new FGAData(startNode.Floor, startNode.Group, startNode.Area),
            //        new FGAData(endNode.Floor, endNode.Group, endNode.Area)));
            //}

            return edges;
        }


        private static Dictionary<GraphNode, IReadOnlyList<FGAEdgeData>> BuildGraphEdges(IReadOnlyList<GraphNode> clusters)
        {
            if(clusters == null || clusters.Count == 0) return null;
            
            Dictionary<GraphNode, IReadOnlyList<FGAEdgeData>> graphEdges = new();

            foreach(GraphNode rootNode in clusters)
            {
                List<FGAEdgeData> edges = new();
                var nodeScanStack = new Stack<GraphNode>();
                var checkedNodes = new HashSet<GraphNode>(); // 체크된 노드들 (순환 탐지)

                nodeScanStack.Push(rootNode); // BFS 시작
                while(nodeScanStack.Count > 0)
                {
                    GraphNode curNode = nodeScanStack.Pop(); // 큐에서 꺼내기
                    if(checkedNodes.Contains(curNode)) continue;
                    checkedNodes.Add(curNode); // 체크된 노드에 추가

                    foreach(GraphNode nextNode in curNode.Children)
                    {
                        edges.Add(new FGAEdgeData(new FGAData(curNode.Floor, curNode.Group, curNode.Area),
                            new FGAData(nextNode.Floor, nextNode.Group, nextNode.Area), !nextNode.Children.Contains(curNode)));
                        nodeScanStack.Push(nextNode); // 큐에 추가
                    }
                }

                graphEdges.Add(rootNode, edges);
            }

            return graphEdges;
        }


        [RelayCommand] private void ClearAnalysisResult()
        {
            analysisGeneration = new object();
            Result = null;
            IntraGroupEdges = null;
            GraphEdges = null;
            SelectedFGAViewLandmarkItem = null;
            SelectedMapViewLandmarkItem = null;
            SelectedFGAViewAreaItem = null;
            SelectedMapViewAreaItem = null;
            SelectedFloor = null;
            SelectedCluster = null;
            SelectedItemSummary = null;
            AreaPivots.Clear();
            AreaPseudoCenter.Clear();
        }
    }
}
