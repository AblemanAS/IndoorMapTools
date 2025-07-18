using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace IndoorMapTools.ViewModel
{
    public partial class Building : ObservableObject
    {
        public Project ParentProject { get; }

        [ObservableProperty] private string name = "Enter building name";
        [ObservableProperty] private string address = "Enter address";
        [ObservableProperty] private float defaultFloorHeight = 4.0f;
        [ObservableProperty] private Point[] outline = new Point[] {};
        public ObservableCollection<Floor> Floors { get; } = new ObservableCollection<Floor>();
        public ObservableCollection<LandmarkGroup> LandmarkGroups { get; } = new ObservableCollection<LandmarkGroup>();

        public Building(Project parentProject) => ParentProject = parentProject;

        // 맵 에디터 핸들러
        [RelayCommand] private void CreateFloor(string filePath)
        {
            Point lonlat = AppVM.Current.DefaultLonLat;
            Floors.Add(new Floor(this, filePath) { LeftLongitude = lonlat.X, BottomLatitude = lonlat.Y });
        }

        public LandmarkGroup CreateLandmarkGroup(LandmarkType type)
        {
            var group = new LandmarkGroup(this, type);
            LandmarkGroups.Add(group);
            return group;
        }

        public Landmark CreateLandmark(LandmarkGroup group, Floor floor, Point position)
        {
            var newLandmark = new Landmark(group, floor, position);
            group.Landmarks.Add(newLandmark);
            return newLandmark;
        }

        [RelayCommand] private void BatchFloorName()
        {
            string tempPrefix = System.Guid.NewGuid().ToString();
            for(int i = 0; i < Floors.Count; i++)
                Floors[i].Name = tempPrefix + (i + 1);
            for(int i = 0; i < Floors.Count; i++)
                Floors[i].Name = "Floor" + (i + 1);
        }

        [RelayCommand] private void BatchLandmarkName()
        {
            // 임시 이름 할당
            string tempPrefix = System.Guid.NewGuid().ToString();
            int tempPostfix = 0;
            foreach(var group in LandmarkGroups)
                foreach(var lm in group.Landmarks)
                    lm.Name = tempPrefix + tempPostfix++;

            var nameDict = new Dictionary<string, int>();
            foreach(var group in LandmarkGroups)
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

        [RelayCommand] private void SortLandmarks()
        {
            LandmarkGroups.OrderBy(group => (int)group.Type);
            foreach(var group in LandmarkGroups)
                group.Landmarks.OrderBy(lm => Floors.IndexOf(lm.ParentFloor));
        }

        public override string ToString() => Name;
    }
}
