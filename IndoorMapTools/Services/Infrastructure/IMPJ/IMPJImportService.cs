using IndoorMapTools.Core;
using IndoorMapTools.Helper;
using IndoorMapTools.Model;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using Bitmap = System.Drawing.Bitmap;

namespace IndoorMapTools.Services.Infrastructure.IMPJ
{
    public class IMPJImportService
    {
        public Project Import(string filePath, Action<int> progressCb = null)
        {
            Project resultProject = new Project();

            string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var progBox = new IntegerProgressBox((p) => progressCb?.Invoke(p));

            try
            {
                // 임시 디렉토리 생성
                var tempDirInfo = Directory.CreateDirectory(tempDirectory);
                ZipFile.ExtractToDirectory(filePath, tempDirectory);

                // Building Attributes 역직렬화
                resultProject.CRS = DeserializeBuildingAttr(resultProject.Building, File.ReadAllText(Path.Combine(tempDirectory, IMPJDefinitions.META_ATTR_FILE_NAME)));
                progBox.Report(10);

                // Floors
                var floorLevelDict = new ConcurrentDictionary<Floor, int>();
                var dirs = tempDirInfo.GetDirectories();
                int maximumProgress = dirs.Length + 1;
                int currentProgress = 0;

                Parallel.ForEach(tempDirInfo.EnumerateDirectories(), floorDir =>
                {
                    // Map Image 로 Floor 생성
                    Floor curFloor = new Floor(resultProject.Building, 
                        ImageAlgorithms.BitmapImageFromFile(Path.Combine(floorDir.FullName, IMPJDefinitions.FLOOR_IMAGE_FILE_NAME)), 0.0, 0.0);
                    resultProject.Building.AddFloor(curFloor);

                    // Floor Attributes 역직렬화
                    floorLevelDict[curFloor] = DeserializeFloorAttr(curFloor, 
                        File.ReadAllText(Path.Combine(floorDir.FullName, IMPJDefinitions.FLOOR_ATTR_FILE_NAME)), resultProject.CRS);

                    // Reachable 언팩
                    Bitmap rawBitmap = new Bitmap(Path.Combine(floorDir.FullName, IMPJDefinitions.FLOOR_OGM_FILE_NAME));
                    curFloor.Reachable = ReachableAlgorithms.BuildReachablefromOGM(rawBitmap, curFloor.MapImage.PixelWidth, curFloor.MapImage.PixelHeight);

                    progBox.Report(10 + 90 * ++currentProgress / maximumProgress);
                });

                // Floor 순서 정렬
                resultProject.Building.SortFloors(fl => floorLevelDict[fl]);

                // Landmarks Attributes 역직렬화
                DeserializeLandmarkGroupsAttr(resultProject.Building, File.ReadAllText(Path.Combine(tempDirectory, IMPJDefinitions.LANDMARKS_ATTR_FILE_NAME)));
                progBox.Report(100);

                // NameSpace 입력
                var allLandmarks = new List<Landmark>();
                foreach(Floor curFloor in resultProject.Building.Floors)
                    foreach(Landmark curLandmark in curFloor.Landmarks)
                        allLandmarks.Add(curLandmark);

                resultProject.Namespace[nameof(Floor)] = GetHighestId(resultProject.Building.Floors, nameof(Floor));
                foreach(string typeStr in Enum.GetNames(typeof(LandmarkType)))
                {
                    resultProject.Namespace[typeStr + IMPJDefinitions.IMPORT_GROUP_PREFIX] = 
                        GetHighestId(resultProject.Building.LandmarkGroups, typeStr + IMPJDefinitions.IMPORT_GROUP_PREFIX);
                    resultProject.Namespace[typeStr] = GetHighestId(allLandmarks, typeStr);
                }

            return resultProject;
            }
            catch(Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning); return null; }
            finally { Directory.Delete(tempDirectory, true); }
        }


        // return : epsg
        private int DeserializeBuildingAttr(Building building, string jsonText)
        {
            Dictionary<string, object> jsonDict = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(jsonText);
            building.Name = (string)jsonDict[IMPJDefinitions.PROP_BUILDING_NAME];
            building.Address = (string)jsonDict[IMPJDefinitions.PROP_BUILDING_ADDRESS];

            int crs;
            // IMPJ 호환성!!! #######################
            try { crs = Convert.ToInt32(jsonDict[IMPJDefinitions.PROP_PROJECT_CRS]); } 
            catch { crs = IMPJDefinitions.IMPORT_DEFAULT_EPSG; }

            var rawListOutline = (ArrayList)jsonDict[IMPJDefinitions.PROP_BUILDING_OUTLINE];
            building.Outline = new Point[rawListOutline.Count];
            for(int i = 0; i < rawListOutline.Count; i++)
            {
                var rawNode = (ArrayList)rawListOutline[i];
                var localNode = new Point(Convert.ToDouble(rawNode[0]), Convert.ToDouble(rawNode[1]));
                building.Outline[i] = GeoLocationModule.ProjectToGlobalSystem(localNode, crs);
            }

            return crs;
        }


        private void DeserializeLandmarkGroupsAttr(Building building, string jsonText)
        {
            foreach(Dictionary<string, object> landmarkGroup in new JavaScriptSerializer().Deserialize<ArrayList>(jsonText))
            {
                var curGroupType = (LandmarkType)IMPJDefinitions.GROUP_TYPE_STRING.IndexOf((string)landmarkGroup[IMPJDefinitions.PROP_GROUP_TYPE]);
                LandmarkGroup curGroup = new LandmarkGroup(building, curGroupType);
                building.AddLandmarkGroup(curGroup);

                foreach(Dictionary<string, object> curLandmarkJsonDict in (ArrayList)landmarkGroup[IMPJDefinitions.PROP_GROUP_EXITS])
                {
                    string curLandmarkFloorName = (string)curLandmarkJsonDict[IMPJDefinitions.PROP_LANDMARK_PARENTFLOOR];
                    foreach(var curFloor in building.Floors)
                    {
                        if(curFloor.Name == curLandmarkFloorName)
                        {
                            double mapImageHeight = curFloor.MapImage.PixelHeight - 1;
                            double ppm = curFloor.MapImagePPM;

                            // 랜드마크 픽셀 좌표 계산
                            var curLandmarkLocX = Convert.ToDouble(curLandmarkJsonDict[IMPJDefinitions.PROP_LANDMARK_X]) * ppm;
                            var curLandmarkLocY = mapImageHeight - Convert.ToDouble(curLandmarkJsonDict[IMPJDefinitions.PROP_LANDMARK_Y]) * ppm;

                            // 랜드마크 생성 및 포함
                            var curLandmark = new Landmark(curGroup, curFloor, new Point(curLandmarkLocX, curLandmarkLocY));
                            curGroup.AddLandmark(curLandmark);
                            curFloor.AddLandmark(curLandmark);

                            // 접근 유형
                            curLandmark.AccessType = Convert.ToInt32(curLandmarkJsonDict[IMPJDefinitions.PROP_LANDMARK_ACCESSTYPE]);

                            // 방향
                            var directionList = (ArrayList)curLandmarkJsonDict[IMPJDefinitions.PROP_LANDMARK_DIRECTION];
                            curLandmark.Direction = (int)(Math.Atan2(Convert.ToDouble(directionList[1]),
                                Convert.ToDouble(directionList[0])) * -180.0 / Math.PI);

                            // 외곽선
                            var outlineList = (ArrayList)curLandmarkJsonDict[IMPJDefinitions.PROP_LANDMARK_OUTLINE];
                            curLandmark.Outline = new Point[outlineList.Count];
                            for(int i = 0; i < outlineList.Count; i++)
                            {
                                curLandmark.Outline[i].X = Convert.ToDouble(((ArrayList)outlineList[i])[0]) * ppm;
                                curLandmark.Outline[i].Y = mapImageHeight - Convert.ToDouble(((ArrayList)outlineList[i])[1]) * ppm;
                            }

                            break;
                        }
                    }
                }
            }
        }


        private int DeserializeFloorAttr(Floor floor, string jsonText, int crs)
        {
            var serializer = new JavaScriptSerializer();

            Dictionary<string, object> jsonDict = serializer.Deserialize<Dictionary<string, object>>(jsonText);
            floor.Name = (string)jsonDict[IMPJDefinitions.PROP_FLOOR_NAME];
            floor.Height = Convert.ToSingle(jsonDict[IMPJDefinitions.PROP_FLOOR_HEIGHT]);
            var rawOffset = (ArrayList)jsonDict[IMPJDefinitions.PROP_FLOOR_OFFSET];
            var localOffset = new Point(Convert.ToDouble(rawOffset[0]), Convert.ToDouble(rawOffset[1]));
            var globalOffset = GeoLocationModule.ProjectToGlobalSystem(localOffset, crs);
            floor.LeftLongitude = globalOffset.X;
            floor.BottomLatitude = globalOffset.Y;
            var curFloorImageJsonDict = (Dictionary<string, object>)jsonDict[IMPJDefinitions.PROP_FLOOR_IMAGE];
            floor.MapImagePPM = Convert.ToDouble(curFloorImageJsonDict[IMPJDefinitions.PROP_FLOOR_IMAGE_PPM]);
            return Convert.ToInt32(jsonDict[IMPJDefinitions.PROP_FLOOR_LEVEL]);
        }


        private int GetHighestId(IEnumerable entityList, string suffix)
        {
            int maxEntityId = 0;
            foreach(Entity curEntity in entityList)
            {
                if(curEntity.Name.StartsWith(suffix) && curEntity.Name.Length > suffix.Length)
                {
                    try
                    {
                        int entityNumber = Convert.ToInt32(curEntity.Name.Substring(suffix.Length));
                        if(entityNumber > maxEntityId) maxEntityId = entityNumber;
                    }
                    catch { }
                }
            }

            return maxEntityId;
        }
    }
}
