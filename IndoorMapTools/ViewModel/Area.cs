using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.ViewModel
{
    public partial class Area : ObservableObject
    {
        public int FloorId { get; private set; }
        public int AreaId { get; private set; }
        [ObservableProperty] private BitmapImage reachable;
        [ObservableProperty] private List<GraphNode> nodes = new List<GraphNode>();

        public Area(int floorId, int areaId)
        {
            FloorId = floorId;
            AreaId = areaId;
        }
    }
}
