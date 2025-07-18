using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Core;
using System.Windows;

namespace IndoorMapTools.ViewModel
{
    public partial class Landmark : Entity
    {
        [ObservableProperty] private Floor parentFloor;
        [ObservableProperty] private LandmarkGroup parentGroup;

        [ObservableProperty] private int accessType; // -1:진입, 0:진입/탈출, 1:탈출
        [ObservableProperty] private int direction; // 15도 단위 시계 방향
        [ObservableProperty] private Point[] outline;

        // 에디터 커맨드
        [RelayCommand] private void Replicate()
        {
            foreach(var floor in ParentFloor.ParentBuilding.Floors)
                if(floor != ParentFloor) CopyTo(floor);
        }

        [RelayCommand] private void JoinGroup(LandmarkGroup group)
        {
            if(ParentGroup == group) return;
            if(ParentGroup.Type != group.Type) return;

            ParentGroup.Landmarks.Remove(this);
            group.Landmarks.Add(this);
            if(ParentGroup.Landmarks.Count == 0)
                ParentGroup.Delete();
            ParentGroup = group;
        }

        [RelayCommand] private void LeaveGroup()
            => JoinGroup(ParentGroup.ParentBuilding.CreateLandmarkGroup(ParentGroup.Type));

        [RelayCommand]
        public void Delete()
        {
            ParentFloor.Landmarks.Remove(this);   // Floor에서 나감
            ParentGroup.Landmarks.Remove(this);   // Group에서 나감
            if(ParentGroup.Landmarks.Count == 0)  // 내 Group 비었어?
                ParentGroup.Delete();             // 그럼 너도 죽어
            // Landmark가 없는 Group은 존재할 수 없음
        }

        private Landmark() { }

        public Landmark(LandmarkGroup group, Floor floor, Point location)
        {
            outline = new Point[] { location };
            ParentFloor = floor;
            ParentGroup = group;
            name = floor.ParentBuilding.ParentProject.GetNumberedName(group.Type.ToString());
        }

        public void CopyTo(Floor newFloor)
        {
            var copied = new Landmark(ParentGroup, newFloor, default);
            newFloor.Landmarks.Add(copied);
            ParentGroup.Landmarks.Add(copied);
            copied.AccessType = AccessType;
            copied.Direction = Direction;

            var transformerSource = MathModule.CalculateTransformer(ParentFloor.MapImage.PixelWidth,
                ParentFloor.MapImage.PixelHeight, ParentFloor.MapImageRotation, 1 / ParentFloor.MapImagePPM);
            var transformerTarget = MathModule.CalculateTransformer(newFloor.MapImage.PixelWidth,
                newFloor.MapImage.PixelHeight, newFloor.MapImageRotation, 1 / newFloor.MapImagePPM);
            transformerTarget.Invert();
            var meterVectorFloor = GeoLocationModule.GetTranslationWGS(new Point(newFloor.LeftLongitude, newFloor.BottomLatitude),
                new Point(ParentFloor.LeftLongitude, ParentFloor.BottomLatitude));

            copied.Outline = (Point[])Outline.Clone();

            for(int i = 0; i < Outline.Length; i++)
            {
                Point meterCoord = transformerSource.Transform(Outline[i]) + meterVectorFloor;
                copied.Outline[i] = transformerTarget.Transform(meterCoord);
            }
        }
    }
}
