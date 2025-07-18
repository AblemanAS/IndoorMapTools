using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Core;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.ViewModel
{
    public partial class Floor : Entity
    {
        public Building ParentBuilding { get; private set; }

        // 데이터 영역
        [ObservableProperty] private double leftLongitude;
        [ObservableProperty] private double bottomLatitude;
        [ObservableProperty] private float height = -1.0f; // 0.0f 보다 낮을 경우 Building의 기본값을 사용
        [ObservableProperty] private BitmapImage mapImage;
        [ObservableProperty] private double mapImagePPM = 10.0;
        [ObservableProperty] private double mapImageRotation; // 시계방향 회전 Degree
        [ObservableProperty] private WriteableBitmap reachable;
        public ObservableCollection<Landmark> Landmarks { get; private set; } = new ObservableCollection<Landmark>();

        // 에디터 영역
        [ObservableProperty] private bool visible = true; // 치욕스럽다...

        // 에디터 커맨드
        [RelayCommand] private void Mark(Point position)
            => ReachableModule.SingleMark(MapImage, Reachable, true, position);
        [RelayCommand] private void Unmark(Point position)
            => ReachableModule.SingleMark(MapImage, Reachable, false, position);
        [RelayCommand] private void Regionmark(Rect dragBox)
            => ReachableModule.RegionMark(MapImage, Reachable, true, dragBox);
        [RelayCommand] private void RegionUnmark(Rect dragBox)
            => ReachableModule.RegionMark(MapImage, Reachable, false, dragBox);
        [RelayCommand] private void LocateStair(Point position)
            => Landmarks.Add(ParentBuilding.CreateLandmark(ParentBuilding.CreateLandmarkGroup(LandmarkType.Stair), this, position));
        [RelayCommand] private void LocateElevator(Point position)
            => Landmarks.Add(ParentBuilding.CreateLandmark(ParentBuilding.CreateLandmarkGroup(LandmarkType.Elevator), this, position));
        [RelayCommand] private void LocateEscalator(Point position)
            => Landmarks.Add(ParentBuilding.CreateLandmark(ParentBuilding.CreateLandmarkGroup(LandmarkType.Escalator), this, position));
        [RelayCommand] private void LocateEntrance(Point position)
            => Landmarks.Add(ParentBuilding.CreateLandmark(ParentBuilding.CreateLandmarkGroup(LandmarkType.Entrance), this, position));
        [RelayCommand] private void LocateStation(Point position)
            => Landmarks.Add(ParentBuilding.CreateLandmark(ParentBuilding.CreateLandmarkGroup(LandmarkType.Station), this, position));
        [RelayCommand] private void RemoveHeight() => Height = -1.0f;

        [RelayCommand] private void LocateFloor(Point destination)
        {
            double xMeter = -MapImage.PixelWidth / MapImagePPM * 0.5;
            double yMeter = -MapImage.PixelHeight / MapImagePPM * 0.5;
            Point leftBottomPoint = GeoLocationModule.TranslateWGSPoint(destination, xMeter, yMeter);
            LeftLongitude = leftBottomPoint.X;
            BottomLatitude = leftBottomPoint.Y;
        }

        [RelayCommand] private void CropPadMapImage(Thickness padding)
        {
            Console.WriteLine($"Padding: {padding}");
        }

        [RelayCommand] private void Reorder(int destIndex)
            => ParentBuilding.Floors.Move(ParentBuilding.Floors.IndexOf(this), destIndex);

        [RelayCommand] private void Replicate(int replicationCount)
        {
            for(int i = 0; i < replicationCount; i++)
            {
                var clone = new Floor
                {
                    ParentBuilding = ParentBuilding,
                    LeftLongitude = LeftLongitude,
                    BottomLatitude = BottomLatitude,
                    Height = Height,
                    MapImage = MapImage,
                    MapImagePPM = MapImagePPM,
                    MapImageRotation = MapImageRotation,
                    Name = AppVM.Current.Project.GetNumberedName(nameof(Floor)),
                    Landmarks = new ObservableCollection<Landmark>()
                };

                Application.Current.Dispatcher.Invoke(() =>
                    clone.Reachable = new WriteableBitmap(Reachable));

                ParentBuilding.Floors.Add(clone);

                foreach(Landmark lm in Landmarks) lm.CopyTo(clone);
            }
        }

        [RelayCommand]
        public void Delete()
        {
            while(Landmarks.Count > 0) Landmarks[0].Delete(); // 내 Landmark 다 죽어
            ParentBuilding.Floors.Remove(this);       // 나도 빌딩에서 나감
        }

        public Floor(Building building, string filePath)
        {
            ParentBuilding = building;

            // 필드 초기화
            name = building.ParentProject.GetNumberedName(nameof(Floor));
            mapImage = ImageModule.BitmapImageFromFile(filePath);
            reachable = ReachableModule.CreateReachable(mapImage.PixelWidth, mapImage.PixelHeight);
            ReachableModule.GetArray(mapImage); // 캐싱
        }

        private Floor() { }
    }
}
