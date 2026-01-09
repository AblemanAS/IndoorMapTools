using IndoorMapTools.Core;
using IndoorMapTools.Model;
using System;
using System.Windows;
using System.Windows.Media;

namespace IndoorMapTools.Services.Domain
{
    internal static class EntityOrganizer
    {
        public static Landmark LocateLandmark(LandmarkType type, Floor floor, Point position)
        {
            Building building = floor.ParentBuilding;
            LandmarkGroup group = building.CreateLandmarkGroup(type);
            Landmark landmark = building.CreateLandmark(group, floor, position);
            floor.AddLandmark(landmark);
            return landmark;
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
            Point translatedOffset = GeoLocationModule.TranslateWGSPoint(floorOffsetLonLat, vectorMeter.X, vectorMeter.Y);
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


        public static void ReplicateFloor(Floor floor, int replicationCount)
        {
            Building building = floor.ParentBuilding;
            for(int i = 0; i < replicationCount; i++)
            {
                Floor newFloor = building.CreateFloor(floor.MapImage, new Point(floor.LeftLongitude, floor.BottomLatitude));
                newFloor.MapImagePPM = floor.MapImagePPM;
                newFloor.MapImageRotation = floor.MapImageRotation;
                foreach(Landmark lm in floor.Landmarks) CopyLandmarkToFloor(lm, newFloor);
            }
        }


        public static void CopyLandmarkToEveryFloors(Landmark target)
        {
            foreach(var floor in target.ParentFloor.ParentBuilding.Floors)
                if(floor != target.ParentFloor) CopyLandmarkToFloor(target, floor);
        }


        public static void CopyLandmarkToFloor(Landmark target, Floor newFloor)
        {
            if(newFloor == null) return;

            Building building = newFloor.ParentBuilding;
            Floor oldFloor = target.ParentFloor;
            Landmark newLandmark = building.CreateLandmark(target.ParentGroup, newFloor, default);
            newLandmark.Direction = target.Direction;
            newLandmark.Outline = (Point[])target.Outline.Clone();

            var transformerSource = MathAlgorithms.CalculateTransformer(oldFloor.MapImage.PixelWidth,
                oldFloor.MapImage.PixelHeight, oldFloor.MapImageRotation, 1 / oldFloor.MapImagePPM);
            var transformerTarget = MathAlgorithms.CalculateTransformer(newFloor.MapImage.PixelWidth,
                newFloor.MapImage.PixelHeight, newFloor.MapImageRotation, 1 / newFloor.MapImagePPM);
            transformerTarget.Invert();
            var meterVectorFloor = GeoLocationModule.GetTranslationWGS(
                new Point(newFloor.LeftLongitude, newFloor.BottomLatitude),
                new Point(oldFloor.LeftLongitude, oldFloor.BottomLatitude));

            for(int i = 0; i < newLandmark.Outline.Length; i++)
            {
                Point meterCoord = transformerSource.Transform(newLandmark.Outline[i]) + meterVectorFloor;
                newLandmark.Outline[i] = transformerTarget.Transform(meterCoord);
            }
        }


        public static void IsolateLandmark(Landmark target)
        {
            LandmarkGroup oldGroup = target.ParentGroup;
            Building building = oldGroup.ParentBuilding;
            LandmarkGroup newGroup = building.CreateLandmarkGroup(oldGroup.Type);
            building.MoveLandmarkToGroup(target, newGroup);
        }
    }
}