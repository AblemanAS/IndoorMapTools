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
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;

namespace IndoorMapTools.Model
{
    public partial class Landmark : Entity
    {
        public Floor ParentFloor { get; }
        public LandmarkGroup ParentGroup { get; set; }

        [ObservableProperty] private int accessType; // -1:진입, 0:진입/탈출, 1:탈출
        [ObservableProperty] private int direction; // 15도 단위 시계 방향
        [ObservableProperty] private Point[] outline;

        [RelayCommand] public override void Delete()
        {
            ParentFloor.RemoveLandmark(this);   // Floor에서 나감
            ParentGroup.RemoveLandmark(this);   // Group에서 나감
        }

        internal Landmark(string entityName, LandmarkGroup group, Floor floor, Point location)
        {
            name = entityName;
            ParentGroup = group;
            ParentFloor = floor;
            outline = new Point[] { location };
        }

        private Landmark() { }
    }
}
