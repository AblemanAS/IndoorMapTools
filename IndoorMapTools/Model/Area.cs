using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.Model
{
    public class Area
    {
        public BitmapImage Reachable { get; }
        public int FloorId { get; private set; }
        public int AreaId { get; private set; }
        public List<Landmark> Landmarks { get; }

        public Area(BitmapImage reachable, int floorId, int areaId)
        {
            Reachable = reachable;
            FloorId = floorId;
            AreaId = areaId;
            Landmarks = new();
        }
    }
}
