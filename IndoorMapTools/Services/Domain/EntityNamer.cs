using IndoorMapTools.Model;
using System;
using System.Collections.Generic;

namespace IndoorMapTools.Services.Domain
{
    public class EntityNamer
    {
        public static void BatchFloorName(IList<Floor> floors)
        {
            string tempPrefix = Guid.NewGuid().ToString();
            for(int i = 0; i < floors.Count; i++)
                floors[i].Name = tempPrefix + (i + 1);
            for(int i = 0; i < floors.Count; i++)
                floors[i].Name = "Floor" + (i + 1);
        }
    }
}
