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

namespace IndoorMapTools.Core
{
    /// <summary>
    /// 수학 관련 기능을 제공하는 모듈입니다.
    /// </summary>
    public static class MathAlgorithms
    {
        public static System.Windows.Point CalculatePolygonCenter(IEnumerable<System.Windows.Point> points)
        {
            // null 이면 기본값 반환
            if(points == null) return default;

            // 비어있으면 기본값 반환
            using var enumorPoints = points.GetEnumerator();
            if(!enumorPoints.MoveNext()) return default;

            // 하나 이상일 경우
            var firstPoint = enumorPoints.Current;
            var prevPoint = firstPoint;
            int pointsLength = 1;
            double area = 0;
            double cx = 0;
            double cy = 0;
            double cross;

            // 각 인접 점에 대한 면적 계산
            while(enumorPoints.MoveNext())
            {
                var curPoint = enumorPoints.Current;
                cross = prevPoint.X * curPoint.Y - curPoint.X * prevPoint.Y;
                area += cross;
                cx += (prevPoint.X + curPoint.X) * cross;
                cy += (prevPoint.Y + curPoint.Y) * cross;
                prevPoint = curPoint;
                pointsLength++;
            }

            // 마지막 점과 첫 점에 대한 면적 계산
            cross = prevPoint.X * firstPoint.Y - firstPoint.X * prevPoint.Y;
            area += cross;
            cx += (prevPoint.X + firstPoint.X) * cross;
            cy += (prevPoint.Y + firstPoint.Y) * cross;

            area *= 0.5;

            // 면적이 0이면(퇴행 다각형) 무게중심 계산 불가능하므로 예외 처리
            if(Math.Abs(area) > 1e-10)
            {
                cx /= (6 * area);
                cy /= (6 * area);
                return new System.Windows.Point(cx, cy);
            }

            var calculatedCenter = new System.Windows.Vector(0, 0);
            foreach(var curPoint in points) calculatedCenter += (System.Windows.Vector)curPoint;
            calculatedCenter /= pointsLength;
            return (System.Windows.Point)calculatedCenter;
        }


        /// <summary>
        /// 특정 규격의 이미지를 바운딩 박스 확장 방식으로 회전시킨 후 스케일링하는 시나리오 상에서,
        /// 원본 이미지 내의 래스터 좌표계 상 각 픽셀 점의 위치로부터
        /// 회전 후의 카르테시안 좌표계 상 미터 좌표로의 변환 행렬을 계산합니다.
        /// </summary>
        /// <param name="imageWidth">이미지의 너비</param>
        /// <param name="imageHeight">이미지의 높이</param>
        /// <param name="rotation">회전 각도 (시계방향, degree)</param>
        /// <param name="scale">스케일 비율</param>
        /// <returns>계산된 변환 행렬</returns>
        public static System.Windows.Media.Matrix CalculateTransformer(int imageWidth, int imageHeight, double rotation, double scale)
        {
            var transformer = new System.Windows.Media.Matrix();
            double angle = rotation * Math.PI / 180.0;
            double cosAngle = Math.Cos(angle);
            double sinAngle = Math.Sin(angle);
            float newWidth = (float)(Math.Abs(imageWidth * cosAngle) + Math.Abs(imageHeight * sinAngle));
            float newHeight = (float)(Math.Abs(imageWidth * sinAngle) + Math.Abs(imageHeight * cosAngle));
            transformer.Translate((newWidth - imageWidth) * 0.5, (newHeight - imageHeight) * 0.5);
            transformer.RotateAt(rotation, newWidth * 0.5, newHeight * 0.5);
            transformer.ScaleAt(1.0, -1.0, 0.0, newHeight * 0.5);
            transformer.ScaleAt(scale, scale, 0.0, 0.0);
            return transformer;
        }
    }
}
