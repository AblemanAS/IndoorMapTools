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
using System.Windows;

namespace IndoorMapTools.Services.Domain
{
    internal static class EntityOrganizer
    {
        public static Landmark LocateLandmark(LandmarkType type, Floor floor, Point position, 
            Dictionary<string, int> namespaceMap)
        {
            Building building = floor.ParentBuilding;
            LandmarkGroup group = building.CreateLandmarkGroup(
                EntityNamer.GetNumberedLandmarkGroupName(namespaceMap, type), type);
            Landmark landmark = building.CreateLandmark(
                EntityNamer.GetNumberedLandmarkName(namespaceMap, type), group, floor, position);
            floor.AddLandmark(landmark);
            return landmark;
        }


        public static void ReplicateFloor(Floor floor, int replicationCount, Dictionary<string, int> namespaceMap)
        {
            Building building = floor.ParentBuilding;
            for(int i = 0; i < replicationCount; i++)
            {
                Floor newFloor = building.CreateFloor(EntityNamer.GetNumberedFloorName(namespaceMap), 
                    floor.MapImage, new Point(floor.LeftLongitude, floor.BottomLatitude));
                newFloor.MapImagePPM = floor.MapImagePPM;
                newFloor.MapImageRotation = floor.MapImageRotation;
                foreach(Landmark lm in floor.Landmarks) CopyLandmarkToFloor(lm, newFloor, namespaceMap);
            }
        }


        public static void CopyLandmarkToEveryFloors(Landmark target, Dictionary<string, int> namespaceMap)
        {
            foreach(var floor in target.ParentFloor.ParentBuilding.Floors)
                if(floor != target.ParentFloor) CopyLandmarkToFloor(target, floor, namespaceMap);
        }


        public static void CopyLandmarkToFloor(Landmark target, Floor newFloor, Dictionary<string, int> namespaceMap)
        {
            if(newFloor == null || target.ParentFloor == newFloor) return;

            Building building = newFloor.ParentBuilding;
            Floor oldFloor = target.ParentFloor;
            string entityName = EntityNamer.GetNumberedLandmarkName(namespaceMap, target.ParentGroup.Type);
            Landmark newLandmark = building.CreateLandmark(entityName, target.ParentGroup, newFloor, default);
            newLandmark.Direction = target.Direction;
            newLandmark.Outline = (Point[])target.Outline.Clone();

            EntityCoordAligner.ProjectPolygonAcrossFloors(newLandmark.Outline, oldFloor, newFloor);
        }


        public static void IsolateLandmark(Landmark target, Dictionary<string, int> namespaceMap)
        {
            LandmarkGroup oldGroup = target.ParentGroup;
            Building building = oldGroup.ParentBuilding;
            string entityName = EntityNamer.GetNumberedLandmarkGroupName(namespaceMap, oldGroup.Type);
            LandmarkGroup newGroup = building.CreateLandmarkGroup(entityName, oldGroup.Type);
            building.MoveLandmarkToGroup(target, newGroup);
        }
    }
}