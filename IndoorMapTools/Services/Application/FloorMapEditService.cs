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

using IndoorMapTools.Algorithm;
using IndoorMapTools.Model;
using IndoorMapTools.Services.Domain;
using System;

namespace IndoorMapTools.Services.Application
{
    public class FloorMapEditService
    {
        /// <summary>
        /// 맵 이미지를 상하좌우 패딩/크롭 처리하고, 층 오프셋 및 랜드마크 좌표도 보정
        /// </summary>
        /// <param name="targetFloor">대상 Floor 객체</param>
        /// <param name="leftPad">좌측 패딩/크롭</param>
        /// <param name="topPad">상측 패딩/크롭</param>
        /// <param name="rightPad">우측 패딩/크롭</param>
        /// <param name="bottomPad">하측 패딩/크롭</param>
        public void PadCropMapImage(Floor targetFloor, int leftPad, int topPad, int rightPad, int bottomPad)
        {
            // 패딩/크롭이 없는 경우 탈출
            if((leftPad == 0) && (topPad == 0) && (rightPad == 0) && (bottomPad == 0)) return;

            // 좌표 보정
            EntityCoordAligner.AdjustCoordOnMapPadCrop(targetFloor, leftPad, topPad, rightPad, bottomPad);

            // 대체 이미지 없을 경우 맵 이미지 패딩/크롭 처리
            targetFloor.MapImage = ImageAlgorithms.GetPadCropImage(targetFloor.MapImage, leftPad, topPad, rightPad, bottomPad);

            // 도달 가능 영역 이미지 패딩/크롭 처리
            targetFloor.Reachable = ReachableAlgorithms.GetPadCropReachable(targetFloor.Reachable, leftPad, topPad, rightPad, bottomPad);
        }
    }
}
