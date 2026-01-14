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
using IndoorMapTools.Core;
using IndoorMapTools.Model;
using IndoorMapTools.MVVMExtensions.ComponentModel;
using IndoorMapTools.Services.Application;
using IndoorMapTools.Services.Presentation;
using System;
using System.Windows;

namespace IndoorMapTools.ViewModel
{
    public partial class GlobalMapVM : ObservableObject
    {
        // 서비스
        private readonly BackgroundService backgroundWorker;
        private readonly IResourceStringService stringSvc;

        public GlobalMapVM(BackgroundService backgroundWorker, IResourceStringService stringSvc)
        {
            this.backgroundWorker = backgroundWorker;
            this.stringSvc = stringSvc;
        }

        [ObservableProperty] private Building model;
        [ObservableProperty] private Floor selectedFloor;
        [ObservableProperty] private Point globalMapFocus;
        [ObservableProperty] private double mapImageOpacity = 1.0;
        public PresentationStateMap<Floor, FloorState> FloorStates { get; } = new (_ => new FloorState());


        partial void OnModelChanged(Building newModel)
        {
            // 상태 초기화
            SelectedFloor = null;
            MapImageOpacity = 1.0;

            // 빈 모델 탈출
            if(newModel == null) return;

            // 모델 값에 따른 초기화
            GlobalMapFocus = (newModel.Outline.Length > 0) ? MathAlgorithms.CalculatePolygonCenter(newModel.Outline)
                : new Point(double.Parse(stringSvc["strings.DEFAULT_LONGITUDE"]), double.Parse(stringSvc["strings.DEFAULT_LATITUDE"]));
        }

        [RelayCommand] private void CreateFloor(string filePath)
            => backgroundWorker.Run(() => Model.CreateFloor(ImageAlgorithms.BitmapImageFromFile(filePath), GlobalMapFocus));


        [RelayCommand] private void LocateFloor(Point destination)
        {
            double xMeter = -SelectedFloor.MapImage.PixelWidth / SelectedFloor.MapImagePPM * 0.5;
            double yMeter = -SelectedFloor.MapImage.PixelHeight / SelectedFloor.MapImagePPM * 0.5;
            Point leftBottomPoint = GeoLocationModule.TranslateWGSPoint(destination, xMeter, yMeter);
            SelectedFloor.LeftLongitude = leftBottomPoint.X;
            SelectedFloor.BottomLatitude = leftBottomPoint.Y;
        }

        [RelayCommand] private void RemoveHeight() => SelectedFloor.Height = null;
    }
}
