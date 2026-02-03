/********************************************************************************
Copyright 2026-present Korea Advanced Institute of Science and Technology (KAIST)

Author: Kyuho Son

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
********************************************************************************/

using CommunityToolkit.Mvvm.ComponentModel;
using IndoorMapTools.Helper;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.Model
{
    public partial class Building : ObservableObject
    {
        public Project ParentProject { get; }

        [ObservableProperty] private string name = "Enter building name";
        [ObservableProperty] private string address = "Enter address";
        [ObservableProperty] private float defaultFloorHeight = 4.0f;
        [ObservableProperty] private Point[] outline = new Point[] {};
        public ObservableCollection<Floor> Floors { get; } = new();
        public ObservableCollection<LandmarkGroup> LandmarkGroups { get; } = new();

        public Building(Project parentProject) => ParentProject = parentProject;

        public Floor CreateFloor(string entityName, BitmapImage mapImage, Point lonlat)
        {
            var floor = new Floor(entityName, this, mapImage, lonlat.X, lonlat.Y);
            Floors.Add(floor);
            return floor;
        }

        public void AddFloor(Floor child) => Floors.Add(child);
        public void SortFloors(Func<Floor, int> keySelector) => Floors.Sort(keySelector);
        public void MoveFloor(Floor floor, int destIndex) => Floors.Move(Floors.IndexOf(floor), destIndex);
        public void RemoveFloor(Floor child) => Floors.Remove(child);

        public LandmarkGroup CreateLandmarkGroup(string entityName, LandmarkType type)
        {
            var group = new LandmarkGroup(entityName, this, type);
            LandmarkGroups.Add(group);
            return group;
        }

        public Landmark CreateLandmark(string entityName, LandmarkGroup group, Floor floor, Point position)
        {
            var newLandmark = new Landmark(entityName, group, floor, position);
            group.AddLandmark(newLandmark);
            floor.AddLandmark(newLandmark);
            return newLandmark;
        }

        public void AddLandmarkGroup(LandmarkGroup child) => LandmarkGroups.Add(child);

        public void SortLandmarks()
        {
            LandmarkGroups.Sort(gr => gr.Type);
            foreach(var group in LandmarkGroups) group.SortLandmarks();
        }

        public void MoveLandmarkToGroup(Landmark target, LandmarkGroup newGroup)
        {
            if(newGroup == target.ParentGroup) return;
            if(target.ParentGroup.Type != newGroup.Type) return;
            target.ParentGroup.RemoveLandmark(target);
            newGroup.AddLandmark(target);
            target.ParentGroup = newGroup;
        }

        public void RemoveLandmarkGroup(LandmarkGroup child) => LandmarkGroups.Remove(child);

        public override string ToString() => Name;
        private Building() {}
    }
}
