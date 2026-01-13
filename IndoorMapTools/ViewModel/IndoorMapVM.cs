using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Core;
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
        public LandmarkTreeItemVM Lvm { get; }

        [ObservableProperty] private Floor model;
        [ObservableProperty] private Entity selectedLandmarkViewItem;
        [ObservableProperty] private Landmark selectedMapViewItem;
        [ObservableProperty] private Point indoorMapFocus;
        [ObservableProperty] private bool reachableVisible = true;
        [ObservableProperty] private bool landmarkVisible = true;
        [ObservableProperty] private bool iconVisible = true;
        [ObservableProperty] private bool outlineVisible = true;
        [ObservableProperty] private bool directionVisible = true;

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


        partial void OnModelChanged(Floor oldModel, Floor newModel)
        {
            // 기존 모델의 이벤트 해제 및 새 모델의 이벤트 연결
            if(oldModel != null) oldModel.PropertyChanged -= onModelPropertyChanged;
            if(newModel != null) newModel.PropertyChanged += onModelPropertyChanged;

            // 맵뷰로부터의 전파일 경우 무시
            if(!isSelectionChangeFromLandmarkView)
            {
                SelectedLandmarkViewItem = null;
                SelectedMapViewItem = null;
            }

            grayArray = null;
            if(newModel != null)
            {
                grayArray = ReachableAlgorithms.GetGrayArray(newModel.MapImage); // 룩업 캐시 갱신
                IndoorMapFocus = new Point(newModel.MapImage.PixelWidth / 2, newModel.MapImage.PixelHeight / 2);
            }

            void onModelPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if(e.PropertyName == nameof(Floor.MapImage))
                    grayArray = ReachableAlgorithms.GetGrayArray(Model.MapImage);
            }
        }


        partial void OnSelectedLandmarkViewItemChanged(Entity newValue)
        {
            // 맵뷰로부터의 전파일 경우 무시
            if(isSelectionChangeFromMapView) return;

            // 선택된 랜드마크 (그룹일 경우 null)
            var newSelection = newValue as Landmark;

            // 층이 다를 경우 층 전환, 순환 호출 방지
            if(newSelection != null && model != newSelection.ParentFloor)
            {
                isSelectionChangeFromLandmarkView = true;
                try { Model = newSelection.ParentFloor; }
                finally { isSelectionChangeFromLandmarkView = false; }
            }

            // 맵뷰의 선택이 다를 경우 동기화, 순환 호출 방지
            if(SelectedMapViewItem == newValue) return;
            isSelectionChangeFromLandmarkView = true;
            try { SelectedMapViewItem = newSelection; }
            finally { isSelectionChangeFromLandmarkView = false; }

            // 포커스 변경
            if(newSelection != null) IndoorMapFocus = MathAlgorithms.CalculatePolygonCenter(newSelection.Outline);

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


        // 에디터 커맨드
        [RelayCommand] private void Mark(Point position) => ReachableAlgorithms.SingleMark(Model.Reachable, grayArray, true, position);
        [RelayCommand] private void Unmark(Point position) => ReachableAlgorithms.SingleMark(Model.Reachable, grayArray, false, position);
        [RelayCommand] private void Regionmark(Rect dragBox) => ReachableAlgorithms.RegionMark(Model.Reachable, true, dragBox);
        [RelayCommand] private void RegionUnmark(Rect dragBox) => ReachableAlgorithms.RegionMark(Model.Reachable, false, dragBox);
        [RelayCommand] private void LocateStair(Point position) => SelectedMapViewItem = EntityOrganizer.LocateLandmark(LandmarkType.Stair, Model, position);
        [RelayCommand] private void LocateElevator(Point position) => SelectedMapViewItem = EntityOrganizer.LocateLandmark(LandmarkType.Elevator, Model, position);
        [RelayCommand] private void LocateEscalator(Point position) => SelectedMapViewItem = EntityOrganizer.LocateLandmark(LandmarkType.Escalator, Model, position);
        [RelayCommand] private void LocateEntrance(Point position) => SelectedMapViewItem = EntityOrganizer.LocateLandmark(LandmarkType.Entrance, Model, position);
        [RelayCommand] private void LocateStation(Point position) => SelectedMapViewItem = EntityOrganizer.LocateLandmark(LandmarkType.Station, Model, position);

        // 컨텍스트 메뉴
        [RelayCommand] private void SortLandmarks() => Model?.ParentBuilding.SortLandmarks();
        [RelayCommand] private void BatchLandmarkName() => Model?.ParentBuilding.BatchLandmarkName();
    }
}
