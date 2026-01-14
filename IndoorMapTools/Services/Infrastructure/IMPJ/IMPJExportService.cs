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

using IndoorMapTools.Core;
using IndoorMapTools.Helper;
using IndoorMapTools.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bitmap = System.Drawing.Bitmap;

namespace IndoorMapTools.Services.Infrastructure.IMPJ
{
    public class IMPJExportService
    {
        private readonly ConcurrentDictionary<Floor, Matrix> transformerCache = new ConcurrentDictionary<Floor, Matrix>();

        public void Export(Project context, string filePath, Action<int> progressCb = null)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            int maximumProgress = context.Building.Floors.Count * 20 + 4;
            int currentProgress = 0;

            var progBox = new IntegerProgressBox((p) => progressCb?.Invoke(p));

            // 각 층별 Transform Matrix 캐시
            foreach(var floor in context.Building.Floors)
                floor.Reachable.Dispatcher.Invoke(() =>
                    transformerCache[floor] = MathAlgorithms.CalculateTransformer(floor.Reachable.PixelWidth,
                    floor.Reachable.PixelHeight, floor.MapImageRotation, 1 / floor.MapImagePPM));
            progBox.Report(100 * ++currentProgress / maximumProgress);

            try
            {
                Directory.CreateDirectory(tempDirectory);
                if(File.Exists(filePath)) File.Delete(filePath); // 이미 Save File 존재할 경우 삭제

                Task.Run(() => // Building 데이터
                {
                    File.WriteAllText(Path.Combine(tempDirectory, IMPJDefinitions.META_ATTR_FILE_NAME), SerializeBuildingAttr(context.Building, context.CRS));
                    progBox.Report(100 * ++currentProgress / maximumProgress);
                });

                Task.Run(() => // Landmarks 데이터
                {
                    File.WriteAllText(Path.Combine(tempDirectory, IMPJDefinitions.LANDMARKS_ATTR_FILE_NAME), SerializeLandmarkGroupsAttr(context.Building.LandmarkGroups));
                    progBox.Report(100 * ++currentProgress / maximumProgress);
                });

                Parallel.ForEach(context.Building.Floors, floor => // Floor 데이터
                {
                    var progBoxfloor = new IntegerProgressBox((p) => progBox.Report(100 * ++currentProgress / maximumProgress));

                    string currentFloorDirectory = Path.Combine(tempDirectory, floor.Name);
                    Directory.CreateDirectory(currentFloorDirectory);
                    PackMapImage(floor.MapImage, floor.MapImageRotation, Path.Combine(currentFloorDirectory, IMPJDefinitions.FLOOR_IMAGE_FILE_NAME));
                    progBoxfloor.Report(1);

                    PointCollection[] includedPolygons = null;
                    includedPolygons = new PointCollection[floor.Landmarks.Count];
                    for(int i = 0; i < includedPolygons.Length; i++)
                        includedPolygons[i] = new PointCollection(floor.Landmarks[i].Outline);
                    progBoxfloor.Report(2);

                    var ogm = ReachableAlgorithms.BuildOGMfromReachable(floor.Reachable, floor.MapImageRotation,
                        1.0 / context.ReachableResolution / floor.MapImagePPM, context.ConservativeCellValidation,
                        floor.Landmarks.Select(lm => lm.Outline), new Progress<int>(p => progBoxfloor.Report(p * 18 / 100 + 2)));

                    ogm.Save(Path.Combine(currentFloorDirectory, IMPJDefinitions.FLOOR_OGM_FILE_NAME), ImageFormat.Png);
                    ogm.Dispose();
                    progBox.Report(19);
                    File.WriteAllText(Path.Combine(currentFloorDirectory, IMPJDefinitions.FLOOR_ATTR_FILE_NAME), SerializeFloorAttr(floor, context.ReachableResolution, context.CRS));
                    progBox.Report(20);
                });

                ZipFile.CreateFromDirectory(tempDirectory, filePath);
                progBox.Report(100 * ++currentProgress / maximumProgress);
            }
            catch(Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
            finally
            {
                Directory.Delete(tempDirectory, true);
                transformerCache.Clear();
            }
        }


        private string SerializeBuildingAttr(Building building, int crs)
        {
            JsonBuilder serializer = new JsonBuilder();

            serializer.StartObject();
            {
                serializer.WritePropertyPair(IMPJDefinitions.PROP_BUILDING_NAME, building.Name);
                serializer.WritePropertyPair(IMPJDefinitions.PROP_BUILDING_ADDRESS, building.Address);
                serializer.WritePropertyPair(IMPJDefinitions.PROP_PROJECT_CRS, crs);

                Point calculatedCenter = new Point(0, 0);
                foreach(Point point in building.Outline)
                    calculatedCenter += (Vector)point;
                calculatedCenter.X /= building.Outline.Length;
                calculatedCenter.Y /= building.Outline.Length;
                serializer.WritePropertyPair(IMPJDefinitions.PROP_BUILDING_CENTER, 
                    calculatedCenter, IMPJDefinitions.EXPORT_LONLAT_PRECISION);

                var convertedList = new List<Point>();
                foreach(Point lonlat in building.Outline)
                    convertedList.Add(GeoLocationModule.ProjectToLocalSystem(lonlat, crs));
                serializer.WritePropertyPair(IMPJDefinitions.PROP_BUILDING_OUTLINE, 
                    convertedList, IMPJDefinitions.EXPORT_METER_PRECISION);
            }
            serializer.EndObject();

            return serializer.ToStringAndReset();
        }


        private string SerializeLandmarkGroupsAttr(IEnumerable<LandmarkGroup> groups)
        {
            JsonBuilder serializer = new JsonBuilder();

            serializer.StartList();
            foreach(LandmarkGroup group in groups)
            {
                serializer.StartObject();
                {
                    serializer.WritePropertyPair(IMPJDefinitions.PROP_GROUP_TYPE, IMPJDefinitions.GROUP_TYPE_STRING[(int)group.Type]);
                    serializer.WritePropertyName(IMPJDefinitions.PROP_GROUP_EXITS);

                    serializer.StartList();
                    foreach(Landmark lm in group.Landmarks)
                    {
                        var transformer = transformerCache[lm.ParentFloor];

                        serializer.StartObject();
                        {
                            Point calculatedCenter = MathAlgorithms.CalculatePolygonCenter(lm.Outline); // Center

                            // 필드 계산
                            Point transformedLocation = transformer.Transform(calculatedCenter); // Location
                            double rotatedDirection = (-lm.Direction - lm.ParentFloor.MapImageRotation) * Math.PI / 180.0; // Direction
                            var transformedOutline = new List<Point>();                                 // Outline
                            foreach(Point pt in lm.Outline) transformedOutline.Add(transformer.Transform(pt));

                            serializer.WritePropertyPair(IMPJDefinitions.PROP_LANDMARK_PARENTFLOOR, lm.ParentFloor.Name);
                            serializer.WritePropertyPair(IMPJDefinitions.PROP_LANDMARK_ACCESSTYPE, lm.AccessType);
                            serializer.WritePropertyPair(IMPJDefinitions.PROP_LANDMARK_X, transformedLocation.X, IMPJDefinitions.EXPORT_METER_PRECISION);
                            serializer.WritePropertyPair(IMPJDefinitions.PROP_LANDMARK_Y, transformedLocation.Y, IMPJDefinitions.EXPORT_METER_PRECISION);
                            serializer.WritePropertyPair(IMPJDefinitions.PROP_LANDMARK_DIRECTION, new Point(Math.Cos(rotatedDirection), Math.Sin(rotatedDirection)));
                            serializer.WritePropertyPair(IMPJDefinitions.PROP_LANDMARK_OUTLINE, transformedOutline, IMPJDefinitions.EXPORT_METER_PRECISION);
                        }
                        serializer.EndObject();
                    }
                    serializer.EndList();
                }
                serializer.EndObject();
            }
            serializer.EndList();

            return serializer.ToStringAndReset();
        }


        private string SerializeFloorAttr(Floor floor, double reachableResolution, int crs)
        {
            JsonBuilder serializer = new JsonBuilder();

            serializer.StartObject();
            {
                double angle = -floor.MapImageRotation * Math.PI / 180.0;
                double imageWidth = floor.MapImage.PixelWidth;
                double imageHeight = floor.MapImage.PixelHeight;

                serializer.WritePropertyPair(IMPJDefinitions.PROP_FLOOR_NAME, floor.Name);
                serializer.WritePropertyPair(IMPJDefinitions.PROP_FLOOR_LEVEL, floor.ParentBuilding.Floors.IndexOf(floor) + 1);
                serializer.WritePropertyPair(IMPJDefinitions.PROP_FLOOR_HEIGHT, floor.Height 
                    ?? floor.ParentBuilding.DefaultFloorHeight, IMPJDefinitions.EXPORT_METER_PRECISION);

                double realWidth = imageWidth / floor.MapImagePPM;
                double realHeight = imageHeight / floor.MapImagePPM;

                Point projectedLoc = GeoLocationModule.ProjectToLocalSystem(new Point(floor.LeftLongitude, floor.BottomLatitude), crs);

                double cosT = Math.Cos(angle);
                double sinT = Math.Sin(angle);

                var corners = new Point[] { new Point(0, 0), new Point(realWidth, 0), new Point(0, realHeight), new Point(realWidth, realHeight) };

                double minX = double.PositiveInfinity;
                double minY = double.PositiveInfinity;
                foreach(var corner in corners)
                {
                    var rc = new Point(corner.X * cosT - corner.Y * sinT, corner.X * sinT + corner.Y * cosT);
                    double x = projectedLoc.X + rc.X;
                    double y = projectedLoc.Y + rc.Y;
                    if(x < minX) minX = x;
                    if(y < minY) minY = y;
                }

                serializer.WritePropertyPair(IMPJDefinitions.PROP_FLOOR_OFFSET, new Point(minX, minY), IMPJDefinitions.EXPORT_METER_PRECISION);

                serializer.WritePropertyName(IMPJDefinitions.PROP_FLOOR_IMAGE);
                serializer.StartObject(false);
                {
                    serializer.WritePropertyPair(IMPJDefinitions.PROP_FLOOR_IMAGE_TYPE, IMPJDefinitions.EXPORT_FLOOR_IMAGE_TYPE);

                    // 없애야 할 부분
                    int newWidth = (int)(Math.Abs(imageWidth * Math.Cos(angle)) + Math.Abs(imageHeight * Math.Sin(angle)));
                    int newHeight = (int)(Math.Abs(imageWidth * Math.Sin(angle)) + Math.Abs(imageHeight * Math.Cos(angle)));
                    serializer.WritePropertyPair(IMPJDefinitions.PROP_FLOOR_IMAGE_WIDTH, newWidth);
                    serializer.WritePropertyPair(IMPJDefinitions.PROP_FLOOR_IMAGE_HEIGHT, newHeight);
                    serializer.WritePropertyPair(IMPJDefinitions.PROP_FLOOR_IMAGE_PPM, floor.MapImagePPM);
                }
                serializer.EndObject();

                serializer.WritePropertyName(IMPJDefinitions.PROP_FLOOR_OGM);
                serializer.StartObject(false);
                {
                    serializer.WritePropertyPair(IMPJDefinitions.PROP_FLOOR_OGM_RES, reachableResolution);
                }
                serializer.EndObject();
            }
            serializer.EndObject();

            return serializer.ToStringAndReset();
        }


        private void PackMapImage(BitmapSource source, double rotation, string filePath)
        {
            Bitmap originalBitmap = source.ToBitmap();
            Bitmap rotatedBitmap = ImageAlgorithms.GetRotatedBitmap(originalBitmap, rotation);
            rotatedBitmap.Save(filePath, ImageFormat.Png);
            originalBitmap.Dispose();
            rotatedBitmap.Dispose();
        }
    }
}
