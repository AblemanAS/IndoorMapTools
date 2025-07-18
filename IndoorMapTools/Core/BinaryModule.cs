using IndoorMapTools.ViewModel;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.Core
{
    /// <summary>
    /// 프로젝트 데이터를 바이너리 형식으로 저장하고 로드하는 기능을 제공합니다.
    /// </summary>
    public class BinaryModule
    {
        /// <summary>
        /// 각 뷰모델 클래스별 프로토버프 모델을 정의하는 배열입니다.
        /// Reference 설정이 필요한 필드는 string 처음을 '&'로 시작합니다.
        /// </summary>
        private static readonly string[][] MEMBERS_DEFINITIONS =
        {
                new string[] { typeof(Project)      .FullName, "&Building", "Namespace", "CRS", "ReachableResolution", 
                                                               "ConservativeCellValidation", "DirectedReachableCluster" },
                new string[] { typeof(Building)     .FullName, "Name", "Address", "DefaultFloorHeight", "Outline", "&Floors", "&LandmarkGroups" },
                new string[] { typeof(Floor)        .FullName, "Name", "LeftLongitude", "BottomLatitude", "Height", "MapImage", "MapImagePPM",
                                                               "MapImageRotation", "Reachable", "&Landmarks", "&ParentBuilding" },
                new string[] { typeof(LandmarkGroup).FullName, "Name", "Type", "&Landmarks", "&ParentBuilding" },
                new string[] { typeof(Landmark)     .FullName, "Name", "AccessType", "Direction", "Outline", "&ParentFloor", "&ParentGroup" }
            };

        /// <summary>
        /// 각 뷰모델 클래스별 프로토버프 모델을 정의하는 배열입니다.
        /// </summary>
        private static readonly Dictionary<Type, Type> SURROGATE_DEFINITIONS = new Dictionary<Type, Type>
        {
            { typeof(BitmapImage), typeof(MapImageSurrogate) },
            { typeof(WriteableBitmap), typeof(ReachableSurrogate) },
            { typeof(Point), typeof(PointSurrogate) }
        };

        /// <summary>
        /// 주어진 파일 경로에서 프로젝트 데이터를 로드합니다.
        /// </summary>
        /// <param name="filePath">프로젝트 파일 경로</param>
        /// <returns>로드된 프로젝트 객체</returns>
        public static Project LoadProject(string filePath)
        {
            Project result = null;
            try
            {
                using Stream s = File.Open(filePath, FileMode.Open);
                result = (Project)ProtoBufModel.Deserialize(s, null, typeof(Project));
            } catch(Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
            
            return result;
        }

        /// <summary>
        /// 주어진 파일 경로에 프로젝트 데이터를 저장합니다.
        /// 지정된 경로에 파일이 이미 존재할 경우 삭제 후 저장합니다.
        /// </summary>
        /// <param name="project">저장할 프로젝트 객체</param>
        /// <param name="filePath">저장할 파일 경로</param>
        public static void SaveProject(Project project, string filePath)
        {
            try
            {
                if(File.Exists(filePath)) File.Delete(filePath);
                using Stream s = File.Open(filePath, FileMode.Create);
                ProtoBufModel.Serialize(s, project);
            }
            catch(Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        // 정의된 모델 설정에 따라 ProtoBufModel을 초기화합니다.
        private static RuntimeTypeModel _ProtoBufModel;
        private static RuntimeTypeModel ProtoBufModel
        {
            get
            {
                if(_ProtoBufModel == null)
                {
                    _ProtoBufModel = RuntimeTypeModel.Create();

                    // MEMBERS_DEFINITIONS 에 따라 필드 추가
                    foreach(var members in MEMBERS_DEFINITIONS)
                    {
                        var metadata = _ProtoBufModel.Add(Type.GetType(members[0]), false);
                        for(int i = 1; i < members.Length; i++)
                        {
                            if(members[i] != null && members[i].Length > 0)
                            {
                                if(members[i][0] == '&') // 참조 필드 처리
                                {
                                    if(members[i].Length < 2) continue;
                                    var vMember = metadata.AddField(i, members[i].Substring(1));
                                    vMember.AsReference = true;
                                }
                                else metadata.AddField(i, members[i]);
                            }
                        }
                    }

                    // SURROGATE_DEFINITIONS 에 따라 서로게이트 추가
                    foreach(var surrogate in SURROGATE_DEFINITIONS)
                        _ProtoBufModel.Add(surrogate.Key, false).SetSurrogate(surrogate.Value);
                }

                return _ProtoBufModel;
            }
        }
    }

    /// <summary>
    /// BitmapImage를 바이너리 형식으로 직렬화하기 위한 서로게이트 클래스입니다.
    /// 바이너리만 직렬화합니다.
    /// </summary>
    [ProtoContract] internal class MapImageSurrogate
    {
        [ProtoMember(1)] public byte[] Binary { get; set; }

        public static explicit operator MapImageSurrogate(BitmapImage source)
        {
            if(source == null) return null;

            MapImageSurrogate surrogate = new MapImageSurrogate();

            using MemoryStream stream = new MemoryStream();
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            encoder.Save(stream);
            surrogate.Binary = stream.ToArray();

            return surrogate;
        }

        public static explicit operator BitmapImage(MapImageSurrogate surrogate)
            => (surrogate == null || surrogate.Binary == null) ? null : ImageModule.BitmapImageFromBuffer(surrogate.Binary);
    }

    /// <summary>
    /// WriteableBitmap을 바이너리 형식으로 직렬화하기 위한 서로게이트 클래스입니다.
    /// Bitmap 바이너리 및 PixelWidth, PixelHeight로 직렬화합니다.
    /// </summary>
    [ProtoContract] internal class ReachableSurrogate // 96DPI, Indexed8
    {
        [ProtoMember(1)] public byte[] Binary { get; set; }
        [ProtoMember(2)] public int PixelWidth { get; set; }
        [ProtoMember(3)] public int PixelHeight { get; set; }

        public static explicit operator ReachableSurrogate(WriteableBitmap source)
        {
            if(source == null) return null;

            ReachableSurrogate surrogate = new ReachableSurrogate();

            source.Dispatcher.Invoke(() =>
            {
                surrogate.Binary = source.ToArray();
                surrogate.PixelWidth = source.PixelWidth;
                surrogate.PixelHeight = source.PixelHeight;
            });

            return surrogate;
        }

        public static explicit operator WriteableBitmap(ReachableSurrogate surrogate)
        {
            if(surrogate == null || surrogate.Binary == null) return null;

            WriteableBitmap reachable = ReachableModule.CreateReachable(surrogate.PixelWidth, surrogate.PixelHeight);
            reachable.Dispatcher.Invoke(() => reachable.FromArray(surrogate.Binary));
            return reachable;
        }
    }

    /// <summary>
    /// Point를 직렬화하기 위한 서로게이트 클래스입니다.
    /// X, Y를 각각 double로 직렬화합니다.
    /// </summary>
    [ProtoContract] internal class PointSurrogate
    {
        [ProtoMember(1)] public double X { get; set; }
        [ProtoMember(2)] public double Y { get; set; }

        public static explicit operator PointSurrogate(Point point) => new PointSurrogate { X = point.X, Y = point.Y };
        public static explicit operator Point(PointSurrogate surrogate) => new Point(surrogate.X, surrogate.Y);
    }
}
