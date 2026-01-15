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

using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Helper;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace IndoorMapTools.Model
{
    public enum LandmarkType { Stair, Elevator, Escalator, Entrance, Station };

    public partial class LandmarkGroup : Entity
    {
        public Building ParentBuilding { get; }

        // 데이터 영역
        public LandmarkType Type { get; }
        public ObservableCollection<Landmark> Landmarks { get; } = new();

        internal LandmarkGroup(string entityName, Building building, LandmarkType type)
        {
            Name = entityName;
            ParentBuilding = building;
            Type = type;
            Landmarks.CollectionChanged += (s, e) =>
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Landmarks)));
        }


        public void SortLandmarks()
        {
            var floors = ParentBuilding.Floors;
            Landmarks.Sort(lm => floors.IndexOf(lm.ParentFloor));
        }

        public void AddLandmark(Landmark child) => Landmarks.Add(child);

        public void RemoveLandmark(Landmark child)
        {
            Landmarks.Remove(child);
            // 가진 Landmark 가 모두 없어지면 자신도 사라짐
            if(Landmarks.Count == 0) Delete();
        }

        [RelayCommand] public override void Delete()
        {
            // 소유 중인 Landmark를 모두 버린 뒤 Building에서 나감
            while(Landmarks.Count > 0) Landmarks[0].Delete();
            ParentBuilding.RemoveLandmarkGroup(this);
        }

        private LandmarkGroup() { }
    }
}
