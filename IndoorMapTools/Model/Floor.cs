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
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Algorithm;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.Model
{
    public partial class Floor : Entity
    {
        public Building ParentBuilding { get; }

        // 데이터 영역
        [ObservableProperty] private double leftLongitude;
        [ObservableProperty] private double bottomLatitude;
        [ObservableProperty] private float? height = null;
        [ObservableProperty] private BitmapImage mapImage;
        [ObservableProperty] private double mapImagePPM = 10.0;
        [ObservableProperty] private double mapImageRotation; // 반시계방향 회전 Degree
        [ObservableProperty] private WriteableBitmap reachable;

        public ObservableCollection<Landmark> Landmarks { get; } = new();

        public void AddLandmark(Landmark child) => Landmarks.Add(child);
        public void RemoveLandmark(Landmark child) => Landmarks.Remove(child);

        [RelayCommand] public override void Delete()
        {
            // 소유 중인 Landmark를 모두 버린 뒤 Building에서 나감
            while(Landmarks.Count > 0) Landmarks[0].Delete();
            ParentBuilding.RemoveFloor(this);
        }


        internal Floor(string entityName, Building building, BitmapImage mapImage, double leftLongitude, double bottomLatitude)
        {
            name = entityName;
            ParentBuilding = building;
            this.mapImage = mapImage;
            this.leftLongitude = leftLongitude;
            this.bottomLatitude = bottomLatitude;
            reachable = ReachableAlgorithms.CreateReachable(mapImage.PixelWidth, mapImage.PixelHeight);
        }

        // kmtproj 파일 포맷 호환성 유지를 위한 후처리
        partial void OnHeightChanged(float? value) { if(height.HasValue && height.Value < 0f) Height = null; }
        private Floor() { }
    }
}
