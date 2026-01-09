using IndoorMapTools.Model;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.Services.Infrastructure.ProtoBuf
{
    internal static class ProtoBufDefinitions
    {
        /// <summary>
        /// 각 뷰모델 클래스별 프로토버프 모델을 정의하는 배열
        /// <para>Reference 설정이 필요한 필드는 string 처음을 '&'로 시작</para>
        /// <para>주의: 직렬화 호환성을 위해 Property들의 인덱스를 절대 변경하지 말 것. 
        /// 기존 Property의 삭제가 필요하다면 해당 인덱스를 null로 표기.</para>
        /// </summary>
        internal static readonly (Type type, string[] members)[] MEMBERS_DEFINITIONS =
        {
            (typeof(Project),       new string[] {"&Building", "Namespace", "CRS", "ReachableResolution",
                                                  "ConservativeCellValidation", "DirectedReachableCluster" }),
            (typeof(Building),      new string[] {"Name", "Address", "DefaultFloorHeight", "Outline", "&Floors", "&LandmarkGroups" }),
            (typeof(Floor),         new string[] {"Name", "LeftLongitude", "BottomLatitude", "Height", "MapImage", "MapImagePPM",
                                                  "MapImageRotation", "Reachable", "&Landmarks", "&ParentBuilding" }),
            (typeof(LandmarkGroup), new string[] {"Name", "Type", "&Landmarks", "&ParentBuilding" }),
            (typeof(Landmark),      new string[] {"Name", "AccessType", "Direction", "Outline", "&ParentFloor", "&ParentGroup"})
        };

        /// <summary>
        /// 각 뷰모델 클래스별 프로토버프 모델을 정의하는 배열
        /// </summary>
        internal static readonly Dictionary<Type, Type> SURROGATE_DEFINITIONS = new Dictionary<Type, Type>
        {
            { typeof(BitmapImage),      typeof(MapImageSurrogate) },
            { typeof(WriteableBitmap),  typeof(ReachableSurrogate) },
            { typeof(Point),            typeof(PointSurrogate) }
        };

    }
}
