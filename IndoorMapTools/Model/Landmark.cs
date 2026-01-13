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

        public Landmark(LandmarkGroup group, Floor floor, Point location)
        {
            outline = new Point[] { location };
            ParentFloor = floor;
            ParentGroup = group;
            name = floor.ParentBuilding.ParentProject.GetNumberedName(group.Type.ToString());
        }

        private Landmark() { }
    }
}
