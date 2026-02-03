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

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FGAView.Controls
{
    public class FGALayoutEngine : IFGALayoutMapper
    {
        private class FGAIds 
        {
            public FGAIds(IEnumerable<(int Floor, int Group, int Area)> values) 
                => this.values = new List<(int Floor, int Group, int Area)>(values);
            public void ChangeValues(IEnumerable<(int Floor, int Group, int Area)> newValues)
            { values.Clear(); values.AddRange(newValues); }
            private readonly List<(int Floor, int Group, int Area)> values;
            public IReadOnlyList<(int Floor, int Group, int Area)> Values => values;
        }

        private Size cellSize = new Size(64, 64); // Cell Size (픽셀 단위) - 기본값 64px

        // Element 예약 및 Layout Map 렌더링 변수
        private readonly ConditionalWeakTable<UIElement, FGAIds> reservationRequests;
        private readonly List<WeakReference<UIElement>> requesters;
        private readonly Dictionary<(int Floor, int Area), int> floorAreaToRow;
        private readonly Dictionary<int, int> groupToColumn;
        private int maxRow = 0;
        private int maxColumn = 0;
        private bool isLayoutMapValid;


        public FGALayoutEngine()
        {
            reservationRequests = new ConditionalWeakTable<UIElement, FGAIds>();
            requesters = new List<WeakReference<UIElement>>();
            floorAreaToRow = new Dictionary<(int Floor, int Area), int>();
            groupToColumn = new Dictionary<int, int>();
        }


        public Size MeasureFGALayout(Size cellSize)
        {
            this.cellSize = cellSize;
            if(isLayoutMapValid) return new Size((maxColumn + 1) * this.cellSize.Width, (maxRow + 1) * this.cellSize.Height);

            floorAreaToRow.Clear();
            groupToColumn.Clear();

            var groupStack = new SortedSet<int>();
            var floorChunks = new SortedDictionary<int, List<(int Group, int Area)>>();

            // 1. 아이템 정보 수집 (Floor 기준으로 묶기)
            for(int i = requesters.Count - 1; i >= 0; --i)
            {
                // Garbage Collection: WeakReference가 해제된 경우
                if(!requesters[i].TryGetTarget(out var curElem))
                {
                    requesters.RemoveAt(i);
                    continue;
                }

                if(!reservationRequests.TryGetValue(curElem, out FGAIds identifiers)) continue;

                foreach(var (floor, group, area) in identifiers.Values)
                {
                    if(floor < 0 || group < 0 || area < 0) continue;

                    if(!floorChunks.TryGetValue(floor, out var gaList))
                    {
                        gaList = new List<(int Group, int Area)>();
                        floorChunks[floor] = gaList;
                    }

                    gaList.Add((group, area));
                    groupStack.Add(group);
                }
            }

            // Group Stack을 Column으로 매핑
            int col = 0;
            foreach(int g in groupStack)
                groupToColumn[g] = col++;

            // 2. Floor 단위로 행 배치 로직 (interval partitioning 알고리즘)
            int globalRow = 0;
            foreach(var kvp in floorChunks) // Floor 오름차순
            {
                // 각 Area 별 Group 범위 모으기
                var gaList = kvp.Value;
                var intervalByArea = new Dictionary<int, (int minG, int maxG)>();
                foreach(var (group, area) in gaList)
                {
                    if(!intervalByArea.TryGetValue(area, out var it)) intervalByArea[area] = (group, group);
                    else intervalByArea[area] = (Math.Min(it.minG, group), Math.Max(it.maxG, group));
                }

                // 각 Area별 interval (area, start, end) 튜플 리스트 생성 후  start 기반 오름차순 정렬
                var intervals = new List<(int area, int start, int end)>(intervalByArea.Count);
                foreach(var p in intervalByArea) intervals.Add((p.Key, p.Value.minG, p.Value.maxG));
                intervals.Sort((a, b) =>
                {
                    int c = a.start.CompareTo(b.start);
                    return c != 0 ? c : a.end.CompareTo(b.end); // tie-breaker로서 end도 참고
                });

                int uniq = 0; // 의미 없음 - 단순히 SortedSet을 충돌 없이 사용하기 위해 부여되는 별도 값
                // 현 interval start group id 에서 점유된 row (end : 어디까지 점유인지, 어느 row, dummy)
                var occupiedRows = new SortedSet<(int end, int row, int u)>();
                var freeRows = new SortedSet<int>(); // 비어 있는 row

                int rowsUsed = 0; // 이 층에서 점유된 총 row 수
                var areaToRow = new Dictionary<int, int>(intervalByArea.Count); // area -> row 맵

                // start 기준으로 오름차순 정렬되어 있는 interval들에 대해 차례로 전개
                // group id가 차근차근 증가해나가며 rows들을 검사해 나가는 로직처럼 작동
                foreach(var (area, start, end) in intervals)
                {
                    // 점유된 row 들에 대해 free 여부 검사 (현재 interval의 요구 group start index와 비교)
                    while(occupiedRows.Count > 0)
                    {
                        var m = occupiedRows.Min;
                        if(m.end >= start) break;   // end가 현재 interval과 겹친다 == 아직 점유되어 있다, 더 검사할 필요 없음
                        occupiedRows.Remove(m);     // 점유되지 않았으면
                        freeRows.Add(m.row);        // freeRows로 옮김
                    }

                    int row;
                    if(freeRows.Count > 0)
                    {
                        row = freeRows.Min;         // 빈 row 가 있으면 거기다 넣고
                        freeRows.Remove(row);       // freeRows에서 제외
                    }
                    else row = rowsUsed++;          // 빈 row 가 없으면 확장

                    areaToRow[area] = row;          // area별 할당된 row 기록해 놓음
                    occupiedRows.Add((end, row, uniq++)); // 방금 할당한 row 를 일단 점유로 분류
                }

                // 실제 (floor, area) → 전역 row 매핑 구성
                foreach(var p in areaToRow) floorAreaToRow[(kvp.Key, p.Key)] = globalRow + p.Value;
                globalRow += rowsUsed; // 다음 Floor 시작 행 갱신
            }

            maxRow = globalRow - 1;
            maxColumn = groupToColumn.Count - 1;
            isLayoutMapValid = true;

            return new Size((maxColumn + 1) * this.cellSize.Width, (maxRow + 1) * this.cellSize.Height);
        }


        public Rect GetItemLayoutRect(int floor, int group, int area)
        {
            if(floor < 0 || group < 0 || area < 0)
                return new Rect(0, 0, cellSize.Width, cellSize.Height);

            if(floorAreaToRow.TryGetValue((floor, area), out int row) &&
                groupToColumn.TryGetValue(group, out int column))
            {
                double x = column * cellSize.Width;
                double y = (maxRow - row) * cellSize.Height; // 행은 아래서부터 위로 올라가므로 Y는 반대로 계산
                return new Rect(x, y, cellSize.Width, cellSize.Height);
            }

            return new Rect(0, 0, cellSize.Width, cellSize.Height);
        }


        public void UpdateReservation(UIElement host, IEnumerable<(int Floor, int Group, int Area)> identifiers)
        {
            if(!reservationRequests.TryGetValue(host, out FGAIds idContainer)) // 신규등록
            {
                requesters.Add(new WeakReference<UIElement>(host));
                reservationRequests.Add(host, new FGAIds(identifiers));
            }
            else idContainer.ChangeValues(identifiers);

            isLayoutMapValid = false;
        }
    }
}
