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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace FGAView.System.Windows.Controls
{
    [ContentProperty(nameof(Children))]
    public class FGAView : Canvas, IFGALayoutMapper
    {
        // 컨트롤 공용 속성
        public Size CellSize { get; set; } = new Size(64, 64); // Cell Size (픽셀 단위) - 기본값 64px

        // Element 예약 및 Layout Map 렌더링 변수
        private readonly Dictionary<WeakReference<UIElement>, IEnumerable<(int Floor, int Group, int Area)>> reservationTable;
        private readonly Dictionary<(int Floor, int Area), int> floorAreaToRow;
        private readonly Dictionary<int, int> groupToColumn;
        private int maxRow = 0;
        private int maxColumn = 0;
        private bool isLayoutMapValid;

        public FGAView()
        {
            reservationTable = new Dictionary<WeakReference<UIElement>, IEnumerable<(int Floor, int Group, int Area)>>();
            floorAreaToRow = new Dictionary<(int Floor, int Area), int>();
            groupToColumn = new Dictionary<int, int>();

            Background = Brushes.White;
            ClipToBounds = true;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            base.MeasureOverride(availableSize);

            if(!isLayoutMapValid)
            {
                isLayoutMapValid = true;
                BuildLayoutMap();
            }

            return new Size((maxColumn + 1) * CellSize.Width, (maxRow + 1) * CellSize.Height);
        }


        private void BuildLayoutMap()
        {
            floorAreaToRow.Clear();
            groupToColumn.Clear();

            var groupStack = new SortedSet<int>();
            var floorChunks = new SortedDictionary<int, List<(int Group, int Area)>>();
            var garbagedHosts = new List<WeakReference<UIElement>>();

            // 1. 아이템 정보 수집 (Floor 기준으로 묶기)
            foreach(var kvPair in reservationTable)
            {
                // Garbage Collection: WeakReference가 해제된 경우
                if(!kvPair.Key.TryGetTarget(out _))
                {
                    garbagedHosts.Add(kvPair.Key);
                    continue;
                }

                foreach(var (floor, group, area) in kvPair.Value)
                {
                    if(floor < 0 || group < 0 || area < 0) continue;

                    if(!floorChunks.TryGetValue(floor, out var list))
                    {
                        list = new List<(int Group, int Area)>();
                        floorChunks[floor] = list;
                    }

                    list.Add((group, area));

                    if(!groupStack.Contains(group))
                        groupStack.Add(group);
                }
            }

            // Group Stack을 Column으로 매핑
            var groupList = groupStack.ToList();
            for(int i = 0; i < groupStack.Count; i++)
                groupToColumn[groupList[i]] = i;

            // Garbage 삭제
            foreach(var garbagedHost in garbagedHosts)
                reservationTable.Remove(garbagedHost);

            // 2. Floor 단위로 행 배치 로직
            int globalRow = 0;
            foreach(var kvp in floorChunks.OrderBy(k => k.Key)) // Floor 오름차순
            {
                int floor = kvp.Key;
                var items = kvp.Value.OrderBy(i => i.Group).ToList();

                var areaToTrack = new Dictionary<int, (int rowIndex, int lastGroup)>(); // Area별 할당된 행

                foreach(var (group, area) in items)
                {
                    if(areaToTrack.TryGetValue(area, out var existing))
                    {
                        // 이전 그룹과 지금 그룹 사이 선분이 생김
                        int g1 = existing.lastGroup;
                        int g2 = group;

                        foreach(var other in areaToTrack)
                        {
                            if(other.Key == area) continue;

                            int og1 = other.Value.lastGroup;
                            int og2 = group;

                            bool overlap = !(g2 < og1 || og2 < g1);

                            if(overlap)
                            {
                                // 충돌: 해당 Area는 새로운 행으로 분리
                                int newRow = areaToTrack.Values.Max(x => x.rowIndex) + 1;
                                areaToTrack[area] = (newRow, group);
                                break;
                            }
                        }

                        // 충돌 없었으면, 위치 업데이트
                        areaToTrack[area] = (areaToTrack[area].rowIndex, group);
                    }
                    else
                    {
                        // 새 Area → 새 행 (아래에서부터 시작)
                        areaToTrack[area] = (areaToTrack.Count, group);
                    }
                }

                // 실제 (floor, area) → 전역 row 매핑 구성
                foreach(var kvpair in areaToTrack)
                {
                    int area = kvpair.Key;
                    int rowOffset = kvpair.Value.rowIndex;
                    floorAreaToRow[(floor, area)] = globalRow + rowOffset;
                }

                globalRow += areaToTrack.Count; // 다음 Floor 시작 행 갱신
            }

            maxRow = globalRow - 1;
            maxColumn = groupToColumn.Count - 1;
        }

        public Rect GetItemLayoutRect(int floor, int group, int area)
        {
            if(floor < 0 || group < 0 || area < 0) 
                return new Rect(0, 0, CellSize.Width, CellSize.Height);

            if(floorAreaToRow.TryGetValue((floor, area), out int row) &&
                groupToColumn.TryGetValue(group, out int column))
            {
                double x = column * CellSize.Width;
                double y = (maxRow - row) * CellSize.Height; // 행은 아래서부터 위로 올라가므로 Y는 반대로 계산
                return new Rect(x, y, CellSize.Width, CellSize.Height);
            }

            return new Rect(0, 0, CellSize.Width, CellSize.Height);
        }

        public void UpdateReservation(UIElement host, IEnumerable<(int Floor, int Group, int Area)> identifiers)
        {
            reservationTable[new WeakReference<UIElement>(host)] = identifiers;
            isLayoutMapValid = false;
            InvalidateMeasure();
        }
    }
}
