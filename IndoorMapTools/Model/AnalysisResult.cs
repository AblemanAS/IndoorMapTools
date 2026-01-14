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
