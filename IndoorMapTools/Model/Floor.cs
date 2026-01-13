using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Core;
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
        [ObservableProperty] private double mapImageRotation; // 시계방향 회전 Degree
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


        internal Floor(Building building, BitmapImage mapImage, double leftLongitude, double bottomLatitude)
        {
            ParentBuilding = building;

            // 필드 초기화
            name = building.ParentProject.GetNumberedName(nameof(Floor));
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
