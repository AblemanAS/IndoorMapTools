using IndoorMapTools.Helper;
using IndoorMapTools.ViewModel;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bitmap = System.Drawing.Bitmap;

namespace IndoorMapTools.Core
{
    public class IMPJModule
    {
        private static readonly List<string> LANDMARK_TYPE_STRING = new List<string> { "VT_ST", "VT_EV", "VT_ES", "ENT", "STN" };
        private static readonly ConcurrentDictionary<Floor, Matrix> transformers = new ConcurrentDictionary<Floor, Matrix>();

        public static Project Import(string filePath, Action<int> progressCb = null)
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
                resultProject.CRS = DeserializeBuildingAttr(resultProject.Building, File.ReadAllText(Path.Combine(tempDirectory, "meta.json")));
                progBox.Report(10);

                // Floors
                var floorLevelDict = new ConcurrentDictionary<Floor, int>();
                var dirs = tempDirInfo.GetDirectories();
                int maximumProgress = dirs.Length + 1;
                int currentProgress = 0;


                Parallel.ForEach(tempDirInfo.EnumerateDirectories(), floorDir =>
                {
                    // Map Image 로 Floor 생성
                    Floor curFloor = new Floor(resultProject.Building, Path.Combine(floorDir.FullName, "image.png"));
                    resultProject.Building.Floors.Add(curFloor);

                    // Floor Attributes 역직렬화
                    floorLevelDict[curFloor] = DeserializeFloorAttr(curFloor, File.ReadAllText(Path.Combine(floorDir.FullName, "floor.json")), resultProject.CRS);

                    // Reachable 언팩
                    Bitmap rawBitmap = new Bitmap(Path.Combine(floorDir.FullName, "ogm.png"));
                    curFloor.Reachable = ReachableModule.BuildReachablefromOGM(rawBitmap, curFloor.MapImage.PixelWidth, curFloor.MapImage.PixelHeight);

                    progBox.Report(10 + 90 * ++currentProgress / maximumProgress);
                });

                // Floor 순서 정렬
                resultProject.Building.Floors.OrderBy(floor => floorLevelDict[floor]);

                // Landmarks Attributes 역직렬화
                DeserializeLandmarkGroupsAttr(resultProject.Building, File.ReadAllText(Path.Combine(tempDirectory, "landmarks.json")));
                progBox.Report(100);

                // NameSpace 입력
                var allLandmarks = new List<Landmark>();
                foreach(Floor curFloor in resultProject.Building.Floors)
                    foreach(Landmark curLandmark in curFloor.Landmarks)
                        allLandmarks.Add(curLandmark);

                resultProject.Namespace[nameof(Floor)] = GetHighestId(resultProject.Building.Floors, nameof(Floor));
                foreach(string typeStr in Enum.GetNames(typeof(LandmarkType)))
                {
                    resultProject.Namespace[typeStr + "Group"] = GetHighestId(resultProject.Building.LandmarkGroups, typeStr + "Group");
                    resultProject.Namespace[typeStr] = GetHighestId(allLandmarks, typeStr);
                }

            return resultProject;
            }
            catch(Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning); return null; }
            finally { Directory.Delete(tempDirectory, true); }
        }


        public static void Export(Project context, string filePath, Action<int> progressCb = null)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            int maximumProgress = context.Building.Floors.Count * 20 + 4;
            int currentProgress = 0;

            var progBox = new IntegerProgressBox((p) => progressCb?.Invoke(p));

            // 각 층별 Transform Matrix 캐시
            foreach(var floor in context.Building.Floors)
                floor.Reachable.Dispatcher.Invoke(() =>
                    transformers[floor] = MathModule.CalculateTransformer(floor.Reachable.PixelWidth,
                    floor.Reachable.PixelHeight, floor.MapImageRotation, 1 / floor.MapImagePPM));
            progBox.Report(100 * ++currentProgress / maximumProgress);

            try
            {
                Directory.CreateDirectory(tempDirectory);
                if(File.Exists(filePath)) File.Delete(filePath); // 이미 Save File 존재할 경우 삭제

                Task.Run(() => // Building 데이터
                {
                    File.WriteAllText(Path.Combine(tempDirectory, "meta.json"), SerializeBuildingAttr(context.Building, context.CRS));
                    progBox.Report(100 * ++currentProgress / maximumProgress);
                });

                Task.Run(() => // Landmarks 데이터
                {
                    File.WriteAllText(Path.Combine(tempDirectory, "landmarks.json"), SerializeLandmarkGroupsAttr(context.Building.LandmarkGroups));
                    progBox.Report(100 * ++currentProgress / maximumProgress);
                });

                Parallel.ForEach(context.Building.Floors, floor => // Floor 데이터
                {
                    var progBoxfloor = new IntegerProgressBox((p) => progBox.Report(100 * ++currentProgress / maximumProgress));

                    string currentFloorDirectory = Path.Combine(tempDirectory, floor.Name);
                    Directory.CreateDirectory(currentFloorDirectory);
                    PackMapImage(floor.MapImage, floor.MapImageRotation, Path.Combine(currentFloorDirectory, "image.png"));
                    progBoxfloor.Report(1);

                    PointCollection[] includedPolygons = null;
                    includedPolygons = new PointCollection[floor.Landmarks.Count];
                    for(int i = 0; i < includedPolygons.Length; i++)
                        includedPolygons[i] = new PointCollection(floor.Landmarks[i].Outline);
                    progBoxfloor.Report(2);

                    var ogm = ReachableModule.BuildOGMfromReachable(floor.Reachable, floor.MapImageRotation,
                        1.0 / context.ReachableResolution / floor.MapImagePPM, context.ConservativeCellValidation,
                        floor.Landmarks.Select(lm => lm.Outline), (p) => progBoxfloor.Report(p * 18 / 100 + 2));

                    ogm.Save(Path.Combine(currentFloorDirectory, "ogm.png"), ImageFormat.Png);
                    ogm.Dispose();
                    progBox.Report(19);
                    File.WriteAllText(Path.Combine(currentFloorDirectory, "floor.json"), SerializeFloorAttr(floor, context.ReachableResolution, context.CRS));
                    progBox.Report(20);
                });

                ZipFile.CreateFromDirectory(tempDirectory, filePath);
                progBox.Report(100 * ++currentProgress / maximumProgress);
            }
            catch(Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
            finally
            {
                Directory.Delete(tempDirectory, true);
                transformers.Clear();
            }
        }


        private static string SerializeBuildingAttr(Building building, int crs)
        {
            JsonModule serializer = new JsonModule();

            serializer.StartObject();
            {
                serializer.WritePropertyPair("name", building.Name);
                serializer.WritePropertyPair("address", building.Address);
                serializer.WritePropertyPair("epsg", crs);

                Point calculatedCenter = new Point(0, 0);
                foreach(Point point in building.Outline)
                    calculatedCenter += (Vector)point;
                calculatedCenter.X /= building.Outline.Length;
                calculatedCenter.Y /= building.Outline.Length;
                serializer.WritePropertyPair("lonlat", calculatedCenter);

                var convertedList = new List<Point>();
                foreach(Point lonlat in building.Outline)
                    convertedList.Add(GeoLocationModule.ProjectToLocalSystem(lonlat, crs));
                serializer.WritePropertyPair("outline", convertedList, 2);
            }
            serializer.EndObject();

            return serializer.Finalize();
        }

        // return : epsg
        private static int DeserializeBuildingAttr(Building building, string jsonText)
        {
            Dictionary<string, object> jsonDict = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(jsonText);
            building.Name = (string)jsonDict["name"];
            building.Address = (string)jsonDict["address"];

            int crs;
            // IMPJ 호환성!!! #######################
            try { crs = Convert.ToInt32(jsonDict["epsg"]); } catch { crs = 5179; }

            var rawListOutline = (ArrayList)jsonDict["outline"];
            building.Outline = new Point[rawListOutline.Count];
            for(int i = 0; i < rawListOutline.Count; i++)
            {
                var rawNode = (ArrayList)rawListOutline[i];
                var localNode = new Point(Convert.ToDouble(rawNode[0]), Convert.ToDouble(rawNode[1]));
                building.Outline[i] = GeoLocationModule.ProjectToGlobalSystem(localNode, crs);
            }

            return crs;
        }


        private static string SerializeLandmarkGroupsAttr(IEnumerable<LandmarkGroup> groups)
        {
            JsonModule serializer = new JsonModule();

            serializer.StartList();
            foreach(LandmarkGroup group in groups)
            {
                serializer.StartObject();
                {
                    serializer.WritePropertyPair("type", LANDMARK_TYPE_STRING[(int)group.Type]);
                    serializer.WritePropertyName("exits");

                    serializer.StartList();
                    foreach(Landmark lm in group.Landmarks)
                    {
                        var transformer = transformers[lm.ParentFloor];

                        serializer.StartObject();
                        {
                            Point calculatedCenter = MathModule.CalculatePolygonCenter(lm.Outline); // Center

                            // 필드 계산
                            Point transformedLocation = transformer.Transform(calculatedCenter); // Location
                            double rotatedDirection = (-lm.Direction - lm.ParentFloor.MapImageRotation) * Math.PI / 180.0; // Direction
                            var transformedOutline = new List<Point>();                                 // Outline
                            foreach(Point pt in lm.Outline) transformedOutline.Add(transformer.Transform(pt));

                            serializer.WritePropertyPair("floor", lm.ParentFloor.Name);
                            serializer.WritePropertyPair("type", lm.AccessType);
                            serializer.WritePropertyPair("x", transformedLocation.X, 2);
                            serializer.WritePropertyPair("y", transformedLocation.Y, 2);
                            serializer.WritePropertyPair("dir", new Point(Math.Cos(rotatedDirection), Math.Sin(rotatedDirection)));
                            serializer.WritePropertyPair("outline", transformedOutline, 2);
                        }
                        serializer.EndObject();
                    }
                    serializer.EndList();
                }
                serializer.EndObject();
            }
            serializer.EndList();

            return serializer.Finalize();
        }


        private static void DeserializeLandmarkGroupsAttr(Building building, string jsonText)
        {
            foreach(Dictionary<string, object> landmarkGroup in new JavaScriptSerializer().Deserialize<ArrayList>(jsonText))
            {
                var curGroupType = (LandmarkType)LANDMARK_TYPE_STRING.IndexOf((string)landmarkGroup["type"]);
                LandmarkGroup curGroup = new LandmarkGroup(building, curGroupType);
                building.LandmarkGroups.Add(curGroup);

                foreach(Dictionary<string, object> curLandmarkJsonDict in (ArrayList)landmarkGroup["exits"])
                {
                    string curLandmarkFloorName = (string)curLandmarkJsonDict["floor"];
                    foreach(var curFloor in building.Floors)
                    {
                        if(curFloor.Name == curLandmarkFloorName)
                        {
                            double mapImageHeight = curFloor.MapImage.PixelHeight - 1;
                            double ppm = curFloor.MapImagePPM;

                            // 랜드마크 픽셀 좌표 계산
                            var curLandmarkLocX = Convert.ToDouble(curLandmarkJsonDict["x"]) * ppm;
                            var curLandmarkLocY = mapImageHeight - Convert.ToDouble(curLandmarkJsonDict["y"]) * ppm;

                            // 랜드마크 생성 및 포함
                            var curLandmark = new Landmark(curGroup, curFloor, new Point(curLandmarkLocX, curLandmarkLocY));
                            curGroup.Landmarks.Add(curLandmark);
                            curFloor.Landmarks.Add(curLandmark);

                            // 방향
                            var directionList = (ArrayList)curLandmarkJsonDict["dir"];
                            curLandmark.Direction = 15 * (int)(Math.Atan2(Convert.ToDouble(directionList[1]),
                                Convert.ToDouble(directionList[0])) * -12.0 / Math.PI);

                            // 외곽선
                            var outlineList = (ArrayList)curLandmarkJsonDict["outline"];
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

        private static string SerializeFloorAttr(Floor floor, double reachableResolution, int crs)
        {
            JsonModule serializer = new JsonModule();

            serializer.StartObject();
            {
                double angle = floor.MapImageRotation * Math.PI / 180.0;
                double imageWidth = floor.MapImage.PixelWidth;
                double imageHeight = floor.MapImage.PixelHeight;

                serializer.WritePropertyPair("name", floor.Name);
                serializer.WritePropertyPair("level", floor.ParentBuilding.Floors.IndexOf(floor) + 1);
                serializer.WritePropertyPair("height", floor.Height < 0.0f ? floor.ParentBuilding.DefaultFloorHeight : floor.Height);

                double realWidth = imageWidth / floor.MapImagePPM;
                double realHeight = imageHeight / floor.MapImagePPM;

                Point projectedLoc = GeoLocationModule.ProjectToLocalSystem(new Point(floor.LeftLongitude, floor.BottomLatitude), crs);
                Point leftBottomPoint = default;

                if(floor.MapImageRotation <= -90)
                {
                    leftBottomPoint = new Point
                    {
                        X = projectedLoc.X + Math.Cos(angle) * realHeight + Math.Sin(angle) * realWidth,
                        Y = projectedLoc.Y + Math.Sin(angle) * realHeight
                    };
                }
                else if(floor.MapImageRotation <= 0)
                {
                    leftBottomPoint = new Point
                    {
                        X = projectedLoc.X + Math.Sin(angle) * realHeight,
                        Y = projectedLoc.Y
                    };
                }
                else if(floor.MapImageRotation <= 90)
                {
                    leftBottomPoint = new Point
                    {
                        X = projectedLoc.X,
                        Y = projectedLoc.Y - Math.Cos(angle) * realWidth
                    };
                }
                else if(floor.MapImageRotation <= 180)
                {
                    leftBottomPoint = new Point
                    {
                        X = projectedLoc.X + Math.Cos(angle) * realWidth - Math.Sin(angle) * realHeight,
                        Y = projectedLoc.Y - Math.Sin(angle) * realWidth + Math.Cos(angle) * realHeight
                    };
                }

                serializer.WritePropertyPair("offset", leftBottomPoint, 2);

                serializer.WritePropertyName("image");
                serializer.StartObject(false);
                {
                    serializer.WritePropertyPair("type", "image/png");

                    // 없애야 할 부분
                    int newWidth = (int)(Math.Abs(imageWidth * Math.Cos(angle)) + Math.Abs(imageHeight * Math.Sin(angle)));
                    int newHeight = (int)(Math.Abs(imageWidth * Math.Sin(angle)) + Math.Abs(imageHeight * Math.Cos(angle)));
                    serializer.WritePropertyPair("width", newWidth);
                    serializer.WritePropertyPair("height", newHeight);
                    serializer.WritePropertyPair("ppm", floor.MapImagePPM);
                }
                serializer.EndObject();

                serializer.WritePropertyName("ogm");
                serializer.StartObject(false);
                {
                    serializer.WritePropertyPair("res", reachableResolution);
                }
                serializer.EndObject();
            }
            serializer.EndObject();

            return serializer.Finalize();
        }

        private static int DeserializeFloorAttr(Floor floor, string jsonText, int crs)
        {
            var serializer = new JavaScriptSerializer();

            Dictionary<string, object> jsonDict = serializer.Deserialize<Dictionary<string, object>>(jsonText);
            floor.Name = (string)jsonDict["name"];
            floor.Height = Convert.ToSingle(jsonDict["height"]);
            var rawOffset = (ArrayList)jsonDict["offset"];
            var localOffset = new Point(Convert.ToDouble(rawOffset[0]), Convert.ToDouble(rawOffset[1]));
            var globalOffset = GeoLocationModule.ProjectToGlobalSystem(localOffset, crs);
            floor.LeftLongitude = globalOffset.X;
            floor.BottomLatitude = globalOffset.Y;
            var curFloorImageJsonDict = (Dictionary<string, object>)jsonDict["image"];
            floor.MapImagePPM = Convert.ToDouble(curFloorImageJsonDict["ppm"]);
            return Convert.ToInt32(jsonDict["level"]);
        }



        private static int GetHighestId(IEnumerable entityList, string suffix)
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


        private static void PackMapImage(BitmapSource source, double rotation, string filePath)
        {
            Bitmap originalBitmap = source.ToBitmap();
            Bitmap rotatedBitmap = ImageModule.GetRotatedBitmap(originalBitmap, rotation);
            rotatedBitmap.Save(filePath, ImageFormat.Png);
            originalBitmap.Dispose();
            rotatedBitmap.Dispose();
        }
    }
}
