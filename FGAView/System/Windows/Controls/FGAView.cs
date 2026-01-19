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
using System.Windows.Media;

namespace FGAView.System.Windows.Controls
{
    /// <summary>
    /// 자기 자신이 FGA 레이아웃 매퍼 기능을 내장한 FGACanvas
    /// </summary>
    public class FGAView : FGACanvas, IFGALayoutMapper
    {
        // 컨트롤 공용 속성
        public Size CellSize { get; set; } = new Size(64, 64); // Cell Size (픽셀 단위) - 기본값 64px
        public IFGALayoutMapper LayoutMapper { get; } = new FGALayoutEngine();


        public FGAView()
        {
            SetCurrentValue(BackgroundProperty, Brushes.White);
            SetCurrentValue(ClipToBoundsProperty,  true);
        }


        protected override Size MeasureOverride(Size availableSize)
        {
            // 전체 하위 control들에 measure 요청
            // 여기서 하위 FGAPanel들에 의해 UpdateReservation가 호출되어 FGA 배치 신청을 모두 받음
            // 배치 신청에 따라 자신의 크기가 달라지기 때문에 Measure에서 처리하는 게 적합
            base.MeasureOverride(availableSize);

            // 맵에 따라 필요한 영역을 계산하여 반환
            return (LayoutMapper as FGALayoutEngine).MeasureFGALayout(CellSize);
        }


        public void UpdateReservation(UIElement item, IEnumerable<(int Floor, int Group, int Area)> identifiers)
        {
            LayoutMapper.UpdateReservation(item, identifiers);
            InvalidateMeasure();
        }


        public Rect GetItemLayoutRect(int floor, int group, int area) => LayoutMapper.GetItemLayoutRect(floor, group, area);
    }
}
