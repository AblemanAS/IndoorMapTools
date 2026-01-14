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
using System.Windows;

namespace IndoorMapTools.Services.Presentation
{
    internal sealed class MessageBoxService : Application.IMessageService
    {
        public void ShowError(string message, string title = null)
            => MessageBox.Show(GetFrontMostModalOwner(), message, title ?? "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        public void ShowInfo(string message, string title = null)
            => MessageBox.Show(GetFrontMostModalOwner(), message, title ?? "Info", MessageBoxButton.OK, MessageBoxImage.Information);

        public bool Confirm(string message, string title = null)
            => MessageBox.Show(GetFrontMostModalOwner(), message, title ?? "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;



        public static Window GetFrontMostModalOwner()
        {
            var app = System.Windows.Application.Current;
            if(app == null) return null;

            var windows = app.Windows;
            if(windows == null || windows.Count == 0) return app.MainWindow;

            // 1) 가장 신뢰도 높은 케이스: 현재 Active 창(대개 최상단 모달)
            for(int i = 0; i < windows.Count; i++)
            {
                if(windows[i] is Window w && w.IsActive)
                    return w;
            }

            // 2) Owner -> (유일한) OwnedChild 매핑 구성 (분기/사이클 방어)
            var childOf = new Dictionary<Window, Window>(ReferenceEqualityComparer<Window>.Instance);
            var branchedOwners = new HashSet<Window>(ReferenceEqualityComparer<Window>.Instance);

            for(int i = 0; i < windows.Count; i++)
            {
                if(!(windows[i] is Window w)) continue;
                var owner = w.Owner;
                if(owner == null) continue;

                if(childOf.TryGetValue(owner, out var existing))
                {
                    if(!ReferenceEquals(existing, w))
                    {
                        branchedOwners.Add(owner); // 분기 발생(가정 위반)
                    }
                }
                else
                {
                    childOf[owner] = w;
                }
            }

            // 3) 후보 루트 선택: MainWindow 우선, 없으면 Owner == null 인 창들 중 “그럴듯한” 것
            Window best = null;

            // (A) MainWindow가 있으면 그 체인을 우선 탐색
            if(app.MainWindow != null)
            {
                best = WalkDown(app.MainWindow, childOf, branchedOwners);
            }

            // (B) 그 외 루트들(Owner == null)도 탐색해서 더 “그럴듯한” 최하단을 선택
            for(int i = 0; i < windows.Count; i++)
            {
                if(!(windows[i] is Window root)) continue;
                if(root.Owner != null) continue; // 루트만

                var deepest = WalkDown(root, childOf, branchedOwners);
                best = PickBetter(best, deepest);
            }

            // (C) 그래도 없으면 아무 창 또는 MainWindow
            return best ?? app.MainWindow ?? (windows.Count > 0 ? windows[0] as Window : null);
        }

        private static Window WalkDown(
            Window start,
            Dictionary<Window, Window> childOf,
            HashSet<Window> branchedOwners)
        {
            var cur = start;

            // 사이클 방지
            var visited = new HashSet<Window>(ReferenceEqualityComparer<Window>.Instance) { cur };

            while(true)
            {
                if(branchedOwners.Contains(cur)) return cur;

                if(!childOf.TryGetValue(cur, out var next)) return cur;
                if(next == null) return cur;

                // Owner 체인은 있으나 표시 상태가 이상한 경우(숨김/닫힘 직전 등) 내려가지 않도록 보수적으로 처리
                if(!next.IsVisible) return cur;

                if(!visited.Add(next)) return cur; // cycle
                cur = next;
            }
        }

        private static Window PickBetter(Window a, Window b)
        {
            if(a == null) return b;
            if(b == null) return a;

            // “앞쪽”의 대체 지표: (1) Visible (2) Not Minimized (3) Topmost (4) Loaded
            int sa = Score(a);
            int sb = Score(b);
            if(sb > sa) return b;
            return a;
        }

        private static int Score(Window w)
        {
            int s = 0;
            if(w.IsVisible) s += 100;
            if(w.WindowState != WindowState.Minimized) s += 10;
            if(w.Topmost) s += 5;
            if(w.IsLoaded) s += 1;
            return s;
        }

        /// <summary>
        /// 참조 동일성 기반 comparer (LINQ/런타임 의존 없이 Dictionary/HashSet에서 Window를 안전하게 키로 쓰기 위함)
        /// </summary>
        private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
        {
            public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();
            public bool Equals(T x, T y) => ReferenceEquals(x, y);
            public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
