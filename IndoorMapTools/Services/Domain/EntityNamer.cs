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


        public static void BatchLandmarkName(IReadOnlyList<LandmarkGroup> landmarkGroups)
        {
            // 임시 이름 할당
            string tempPrefix = System.Guid.NewGuid().ToString();
            int tempPostfix = 0;
            foreach(var group in landmarkGroups)
                foreach(var lm in group.Landmarks)
                    lm.Name = tempPrefix + tempPostfix++;

            var nameDict = new Dictionary<string, int>();
            foreach(var group in landmarkGroups)
                foreach(var lm in group.Landmarks)
                {
                    string curName = group.Name + "-" + lm.ParentFloor.Name;
                    if(!nameDict.ContainsKey(curName))
                    {
                        nameDict[curName] = 0;
                        lm.Name = curName;
                    }
                    else lm.Name = curName + ++nameDict[curName];
                }

        }


        public static string GetNumberedFloorName(Dictionary<string, int> namespaceMap) 
            => GetNumberedName(namespaceMap, nameof(Floor));

        public static string GetNumberedLandmarkName(Dictionary<string, int> namespaceMap, LandmarkType type)
            => GetNumberedName(namespaceMap, type.ToString());

        public static string GetNumberedLandmarkGroupName(Dictionary<string, int> namespaceMap, LandmarkType type)
            => GetNumberedName(namespaceMap, type.ToString() + "Group");

        public static string GetNumberedFloor<T>(Dictionary<string, int> namespaceMap)
            => GetNumberedName(namespaceMap, nameof(T));

        public static string GetNumberedName(Dictionary<string, int> namespaceMap, string prefixName)
        {
            if(!namespaceMap.ContainsKey(prefixName))
            {
                namespaceMap[prefixName] = 1;
                return prefixName + 1;
            }

            return prefixName + ++namespaceMap[prefixName];
        }
    }
}