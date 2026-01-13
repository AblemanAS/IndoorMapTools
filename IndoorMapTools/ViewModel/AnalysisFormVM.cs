using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Core;
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

        [NotifyCanExecuteChangedFor(nameof(AnalyzeReachabilityCommand))]
        [ObservableProperty] private bool areLandmarkOutlinesComplete;

        private bool guardSelectPropagation = false;

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

            SelectedItemSummary = value.ToString();
            Floor newFloor = value?.ParentFloor;
            if(SelectedFloor != newFloor) SelectedFloor = newFloor;
            if(value != null) MapViewFocus = MathAlgorithms.CalculatePolygonCenter(value.Outline);
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

            SelectedItemSummary = value.ToString();
            Floor newFloor = (value != null) ? Model.Building.Floors[value.FloorId] : null;
            if(SelectedFloor != newFloor) SelectedFloor = newFloor;
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

            SelectedItemSummary = value.ToString();
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

            SelectedItemSummary = value.ToString();
            Floor newFloor = (value != null) ? Model.Building.Floors[value.FloorId] : null;
            if(SelectedFloor != newFloor) SelectedFloor = newFloor;
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
                SelectedMapViewAreaItem = null;
            }
            finally { guardSelectPropagation = false; }
        }


        [RelayCommand] private void ValidateLandmarkCompleteness()
            => AreLandmarkOutlinesComplete = EntityValidator.AreLandmarkOutlinesComplete(Model.Building.LandmarkGroups);

        [RelayCommand] private void AnalyzeReachability()
        {
            bgSvc.Run(() =>
            {
                Result = null;
                var result = AnalysisService.AnalyzeReachability(Model.Building, fgaSolver,
                    Model.ReachableResolution, Model.ConservativeCellValidation, Model.DirectedReachableCluster, bgSvc.ReportProgress);
                IntraGroupEdges = BuildIntraGroupEdges(result, Model.Building);
                GraphEdges = BuildGraphEdges(result.ReachableClusters);
                Result = result;
            }, strSvc["strings.ReachableClusterAnalysisStatusDesc"]);
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
            Result = null;
            SelectedFGAViewLandmarkItem = null;
            SelectedMapViewLandmarkItem = null;
            SelectedFloor = null;
            SelectedCluster = null;
        }
    }
}
