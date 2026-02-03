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
using IndoorMapTools.Services.Infrastructure.GeoLocation;
using System;
using System.Windows;
using System.Windows.Media;

namespace IndoorMapTools.Services.Domain
{
    public class EntityCoordAligner
    {
        public static void ProjectPolygonAcrossFloors(Point[] polygon, Floor sourceFloor, Floor targetFloor)
        {
            var transformerSource = CoordTransformAlgorithms.CalculateTransformer(sourceFloor.MapImage.PixelWidth,
                sourceFloor.MapImage.PixelHeight, sourceFloor.MapImageRotation, 1 / sourceFloor.MapImagePPM);
            var transformerTarget = CoordTransformAlgorithms.CalculateTransformer(targetFloor.MapImage.PixelWidth,
                targetFloor.MapImage.PixelHeight, targetFloor.MapImageRotation, 1 / targetFloor.MapImagePPM);
            transformerTarget.Invert();
            var meterVectorFloor = CoordTransformAlgorithms.GetTranslationWGS(
                new Point(targetFloor.LeftLongitude, targetFloor.BottomLatitude),
                new Point(sourceFloor.LeftLongitude, sourceFloor.BottomLatitude));

            for(int i = 0; i < polygon.Length; i++)
            {
                Point meterCoord = transformerSource.Transform(polygon[i]) + meterVectorFloor;
                polygon[i] = transformerTarget.Transform(meterCoord);
            }
        }


        /// <summary>
        /// 층 오프셋 및 랜드마크 좌표를 패딩/크롭에 맞게 보정함.
        /// </summary>
        /// <param name="targetFloor">대상 Floor 객체</param>
        /// <param name="left">좌측 패딩/크롭</param>
        /// <param name="top">상측 패딩/크롭</param>
        /// <param name="right">우측 패딩/크롭 (현재 구현에서는 사용되지 않음)</param>
        /// <param name="bottom">하측 패딩/크롭</param>
        public static void AdjustCoordOnMapPadCrop(Floor targetFloor, int left, int top, int right, int bottom)
        {
            // Floor offset 수정
            Point floorOffsetLonLat = new Point(targetFloor.LeftLongitude, targetFloor.BottomLatitude);
            var rotateTransform = new RotateTransform(-targetFloor.MapImageRotation);
            var vectorMeter = rotateTransform.Transform((Point)(new Vector(-left, -bottom) / targetFloor.MapImagePPM));
            Point translatedOffset = CoordTransformAlgorithms.TranslateWGSPoint(floorOffsetLonLat, vectorMeter.X, vectorMeter.Y);
            targetFloor.LeftLongitude = translatedOffset.X;
            targetFloor.BottomLatitude = translatedOffset.Y;

            // 랜드마크 및 Geofence Translation Matrix
            var translator = new System.Windows.Media.Matrix();
            translator.Translate(left, top);

            // 랜드마크 수정
            foreach(Landmark lm in targetFloor.Landmarks)
                for(int i = 0; i < lm.Outline.Length; i++)
                    lm.Outline[i] = translator.Transform(lm.Outline[i]);
        }
    }
}
