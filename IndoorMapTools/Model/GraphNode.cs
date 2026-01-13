using System;
using System.Collections.Generic;
namespace IndoorMapTools.Model
{
    public class GraphNode
    {
        public int Floor { get; }
        public int Group { get; }
        public int Area { get; }
        public object Data { get; }

        public List<GraphNode> Children { get; } = new List<GraphNode>();
        public GraphNode(int floor, int group, int area, object data)
        { Floor = floor; Group = group; Area = area; Data = data; }
    }
}
