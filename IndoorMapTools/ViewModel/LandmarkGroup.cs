using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace IndoorMapTools.ViewModel
{
    public enum LandmarkType { Stair, Elevator, Escalator, Entrance, Station };

    public partial class LandmarkGroup : Entity
    {
        public Building ParentBuilding { get; }

        // 데이터 영역
        public LandmarkType Type { get; }
        public ObservableCollection<Landmark> Landmarks { get; }

        // 에디터 커맨드
        [RelayCommand]
        public void Delete()
        {
            while(Landmarks.Count > 0) Landmarks[0].Delete();   // 내가 갖고 있는 Landmark 다 죽어
            ParentBuilding.LandmarkGroups.Remove(this);         // 나도 Building에서 나감
        }

        private LandmarkGroup() { }

        public LandmarkGroup(Building building, LandmarkType type)
        {
            ParentBuilding = building;
            Type = type; // 필드 초기화
            Name = building.ParentProject.GetNumberedName(Type.ToString() + "Group"); // 이름 수정

            // 하위 컬렉션 초기화
            Landmarks = new ObservableCollection<Landmark>();
            Landmarks.CollectionChanged += (sender, e) => OnPropertyChanged(nameof(Landmarks));
        }
    }
}
