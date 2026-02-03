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
using IndoorMapTools.Model;
using IndoorMapTools.Services.Domain;
using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;

namespace IndoorMapTools.ViewModel
{
    public partial class IndoorMapVM : ObservableObject
    {
        // MapView 영역
        [ObservableProperty] private Floor mapViewModel;
        [ObservableProperty] private Landmark selectedMapViewItem;
        [ObservableProperty] private Point indoorMapFocus;
        [ObservableProperty] private bool reachableVisible = true;
        [ObservableProperty] private bool landmarkVisible = true;
        [ObservableProperty] private bool iconVisible = true;
        [ObservableProperty] private bool outlineVisible = true;
        [ObservableProperty] private bool directionVisible = true;

        // LandmarkView 영역
        public LandmarkTreeItemVM Lvm { get; }
        [ObservableProperty] private Building treeViewModel;
        [ObservableProperty] private Entity selectedLandmarkViewItem;
        public Func<object, IEnumerable> ChildSelector { get; } = (g) => (g as LandmarkGroup)?.Landmarks;

        private byte[] grayArray;
        private bool isSelectionChangeFromLandmarkView = false;
        private bool isSelectionChangeFromMapView = false;


        public IndoorMapVM(LandmarkTreeItemVM lvm)
        {
            Lvm = lvm;

            PropertyChanged += (sender, e) =>
            {
                // Landmark 요소 공통 Visibility 루틴
                switch(e.PropertyName)
                {
                    case nameof(IconVisible):
                    case nameof(OutlineVisible):
                    case nameof(DirectionVisible):
                        if(IconVisible == OutlineVisible && IconVisible == DirectionVisible && IconVisible != LandmarkVisible)
                            LandmarkVisible = IconVisible;
                        break;

                    case nameof(LandmarkVisible):
                        bool newValueLandmarkVisible = LandmarkVisible;
                        if(IconVisible != newValueLandmarkVisible) IconVisible = newValueLandmarkVisible;
                        if(OutlineVisible != newValueLandmarkVisible) OutlineVisible = newValueLandmarkVisible;
                        if(DirectionVisible != newValueLandmarkVisible) DirectionVisible = newValueLandmarkVisible;
                        break;
                }

            };
        }


        partial void OnMapViewModelChanged(Floor oldValue, Floor newValue)
        {
            // 기존 모델의 이벤트 해제 및 새 모델의 이벤트 연결
            if(oldValue != null) oldValue.PropertyChanged -= onMapViewModelPropertyChanged;
            if(newValue != null) newValue.PropertyChanged += onMapViewModelPropertyChanged;

            // 맵뷰로부터의 전파일 경우 무시
            if(!isSelectionChangeFromLandmarkView)
            {
                SelectedLandmarkViewItem = null;
                SelectedMapViewItem = null;
            }

            grayArray = null;
            if(newValue != null)
            {
                grayArray = ReachableAlgorithms.GetGrayArray(newValue.MapImage); // 룩업 캐시 갱신
                IndoorMapFocus = new Point(newValue.MapImage.PixelWidth / 2, newValue.MapImage.PixelHeight / 2);
            }

            void onMapViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if(e.PropertyName == nameof(Floor.MapImage))
                    grayArray = ReachableAlgorithms.GetGrayArray(MapViewModel.MapImage);
            }
        }


        partial void OnTreeViewModelChanged(Building oldValue, Building newValue)
            => SelectedLandmarkViewItem = null;


        partial void OnSelectedLandmarkViewItemChanged(Entity newValue)
        {
            // 맵뷰로부터의 전파일 경우 무시
            if(isSelectionChangeFromMapView) return;

            // 선택된 랜드마크 (그룹일 경우 null)
            var newSelection = newValue as Landmark;

            // 층이 다를 경우 층 전환, 순환 호출 방지
            if(newSelection != null && mapViewModel != newSelection.ParentFloor)
            {
                isSelectionChangeFromLandmarkView = true;
                try { MapViewModel = newSelection.ParentFloor; }
                finally { isSelectionChangeFromLandmarkView = false; }
            }

            // 맵뷰의 선택이 다를 경우 동기화, 순환 호출 방지
            if(SelectedMapViewItem == newValue) return;
            isSelectionChangeFromLandmarkView = true;
            try { SelectedMapViewItem = newSelection; }
            finally { isSelectionChangeFromLandmarkView = false; }

            // 포커스 변경
            if(newSelection != null) IndoorMapFocus = CoordTransformAlgorithms.CalculatePolygonCenter(newSelection.Outline);

            ReflectLandmarkSelectionSSOT(newSelection);
        }


        partial void OnSelectedMapViewItemChanged(Landmark newValue)
        {
            // 랜드마크 뷰로부터의 전파일 경우 무시
            if(isSelectionChangeFromLandmarkView) return;

            // 랜드마크뷰의 선택이 다를 경우 동기화, 순환 호출 방지
            if(SelectedLandmarkViewItem == newValue) return;
            isSelectionChangeFromMapView = true;
            try { SelectedLandmarkViewItem = newValue; }
            finally { isSelectionChangeFromMapView = false; }

            ReflectLandmarkSelectionSSOT(newValue);
        }

        // Landmark 선택 SSOT 변화를 현재 VM의 뷰 속성들에 반영
        private void ReflectLandmarkSelectionSSOT(Landmark newValue)
        {
            // 하위 뷰모델 선택
            Lvm.Model = newValue;

            // 이후 Landmark 선택 SSOT에 종속적인
            // 뷰모델 또는 속성들이 추가되면 이 곳에 구현
        }


        // MapView 에디터 커맨드
        [RelayCommand] private void Mark(Point position) 
            => ReachableAlgorithms.SingleMark(MapViewModel.Reachable, grayArray, true, position);
        [RelayCommand] private void Unmark(Point position) 
            => ReachableAlgorithms.SingleMark(MapViewModel.Reachable, grayArray, false, position);
        [RelayCommand] private void Regionmark(Rect dragBox) 
            => ReachableAlgorithms.RegionMark(MapViewModel.Reachable, true, dragBox);
        [RelayCommand] private void RegionUnmark(Rect dragBox) 
            => ReachableAlgorithms.RegionMark(MapViewModel.Reachable, false, dragBox);
        [RelayCommand] private void LocateStair(Point position) => SelectedMapViewItem = EntityOrganizer.LocateLandmark(
            LandmarkType.Stair, MapViewModel, position, TreeViewModel.ParentProject.Namespace);
        [RelayCommand] private void LocateElevator(Point position) => SelectedMapViewItem = EntityOrganizer.LocateLandmark(
            LandmarkType.Elevator, MapViewModel, position, TreeViewModel.ParentProject.Namespace);
        [RelayCommand] private void LocateEscalator(Point position) => SelectedMapViewItem = EntityOrganizer.LocateLandmark(
            LandmarkType.Escalator, MapViewModel, position, TreeViewModel.ParentProject.Namespace);
        [RelayCommand] private void LocateEntrance(Point position) => SelectedMapViewItem = EntityOrganizer.LocateLandmark(
            LandmarkType.Entrance, MapViewModel, position, TreeViewModel.ParentProject.Namespace);
        [RelayCommand] private void LocateStation(Point position) => SelectedMapViewItem = EntityOrganizer.LocateLandmark(
            LandmarkType.Station, MapViewModel, position, TreeViewModel.ParentProject.Namespace);

        // TreeView 컨텍스트 메뉴
        [RelayCommand] private void SortLandmarks() => TreeViewModel?.SortLandmarks();
        [RelayCommand] private void BatchLandmarkName() => EntityNamer.BatchLandmarkName(TreeViewModel.LandmarkGroups);
    }
}
