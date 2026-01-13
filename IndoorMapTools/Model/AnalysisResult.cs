using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace IndoorMapTools.Model
{
    public partial class AnalysisResult : ObservableObject
    {
        public List<Area> Areas { get; }
        public List<Landmark> Landmarks { get; }
        public Dictionary<Floor, List<Area>> FloorToAreas { get; }
        public Dictionary<Landmark, GraphNode> LandmarkToNode { get; }
        public List<GraphNode> ReachableClusters { get; }
        public int[] GroupOrder { get; }

        public AnalysisResult(List<Area> areas, List<Landmark> landmarks, Dictionary<Floor, List<Area>> floorToAreas, 
            Dictionary<Landmark, GraphNode> landmarkToNode, List<GraphNode> reachableClusters, int[] groupOrder = null)
        {
            Areas = areas;
            Landmarks = landmarks;
            FloorToAreas = floorToAreas;
            LandmarkToNode = landmarkToNode;
            ReachableClusters = reachableClusters;
            GroupOrder = groupOrder;
        }
    }
}
