using IndoorMapTools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndoorMapTools.Services.Domain
{
    internal static class EntityValidator
    {
        public static bool AreLandmarkOutlinesComplete(IEnumerable<LandmarkGroup> landmarkGroups)
        {
            foreach(var group in landmarkGroups)
                foreach(var landmark in group.Landmarks)
                    if(landmark.Outline.Length < 2)
                        return false;
            return true;
        }
    }
}
