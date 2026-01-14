using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace IndoorMapTools.Model
{
    public partial class AnalysisResult : ObservableObject
    {
        public IReadOnlyList<Area> Areas { get; }
        public IReadOnlyList<Landmark> Landmarks { get; }
        public IReadOnlyDictionary<Floor, List<Area>> FloorToAreas { get; }
        public IReadOnlyDictionary<Landmark, GraphNode> LandmarkToNode { get; }
        public IReadOnlyList<GraphNode> ReachableClusters { get; }
        public int[] GroupOrder { get; }

        public AnalysisResult(IReadOnlyList<Area> areas, IReadOnlyList<Landmark> landmarks, 
            IReadOnlyDictionary<Floor, List<Area>> floorToAreas, IReadOnlyDictionary<Landmark, GraphNode> landmarkToNode, 
            IReadOnlyList<GraphNode> reachableClusters, int[] groupOrder = null)
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
