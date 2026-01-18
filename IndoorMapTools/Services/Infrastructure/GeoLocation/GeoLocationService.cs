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

using DotSpatial.Projections;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace IndoorMapTools.Services.Infrastructure.GeoLocation
{
    public class GeoLocationService
    {
        private const string SRID_PATH = "srid";
        private const int MAX_EPSG = 65535;

        private int epsg = 0;
        private static ProjectionInfo globalSystem;
        private static (string Name, string WKT)[] srids;
        private ProjectionInfo localSystem;

        public GeoLocationService()
        {
            if(globalSystem != null) return;
            srids = SRIDReader.Load(SRID_PATH, MAX_EPSG);

            // 이 부분에서 'System.IO.FileNotFoundException'(mscorlib.dll) 로드 예외 발생
            // DotSpatial.Projections 초기화 시 XML Serialization 관련 dll 참조 오류이나,
            // 본 프로젝트에서는 해당 기능을 전혀 사용하지 않으므로 무시해도 됨
            Debug.WriteLine("=====================================================================================");
            Debug.WriteLine("[INFO] The two FileNotFoundExceptions below are ignorable. (from mscorlib.dll)       ");
            Debug.WriteLine("[INFO] Cause: DotSpatial.Projections XML Serializer scan   (Not used in this project)");
            Debug.WriteLine("=====================================================================================");
            globalSystem = ProjectionInfo.FromEsriString(srids[4326].WKT);
        }

        public string ToName(int epsg) => srids[epsg].Name;
        public string ToWKT(int epsg) => BeautifyWKT(srids[epsg].WKT);

        /// <summary>
        /// EPSG 코드에 해당하는 지역 미터 좌표계를 다시 로드합니다.
        /// </summary>
        /// <param name="epsg">지역 미터 좌표계의 EPSG 코드</param>
        private void ReloadLocalSystem(int epsg)
        {
            if(epsg < 0 || epsg >= srids.Length || this.epsg == epsg) return;
            this.epsg = epsg;
            localSystem = ProjectionInfo.FromEsriString(srids[epsg].WKT);
        }

        /// <summary>
        /// WKT 문자열을 읽기 좋게 변환합니다.
        /// </summary>
        /// <param name="wkt">원본 wkt 문자열</param>
        /// <returns></returns>
        private static string BeautifyWKT(string wkt)
        {
            if(string.IsNullOrEmpty(wkt)) return wkt;

            var sb = new System.Text.StringBuilder();
            int indent = 0;
            bool inQuotes = false; // 쌍따옴표 내에서는 대괄호나 쉼표 포맷팅 무시

            for(int i = 0; i < wkt.Length; i++)
            {
                char c = wkt[i];

                // 따옴표 토글: 문자열 내의 내용은 그대로 출력
                if(c == '\"')
                {
                    inQuotes = !inQuotes;
                    sb.Append(c);
                }
                else if(!inQuotes)
                {
                    if(c == '[')
                    {
                        sb.Append(c);
                        indent++;
                        sb.AppendLine();
                        sb.Append(new string(' ', indent * 4)); // 들여쓰기 (4칸 공백)
                    }
                    else if(c == ']')
                    {
                        indent--;
                        sb.AppendLine();
                        sb.Append(new string(' ', indent * 4));
                        sb.Append(c);
                    }
                    else sb.Append(c);
                }
                else sb.Append(c); // inQuotes 상태에서는 그냥 추가
            }

            return sb.ToString();
        }


        public Point ProjectToLocalSystem(Point lonlat, int epsg)
        {
            if(this.epsg != epsg) ReloadLocalSystem(epsg);
            return Project(lonlat, globalSystem, localSystem);
        }


        public Point[] ProjectToLocalSystem(ICollection<Point> lonlats, int epsg)
        {
            if(this.epsg != epsg) ReloadLocalSystem(epsg);
            return Project(lonlats, globalSystem, localSystem);
        }


        public Point ProjectToGlobalSystem(Point meter, int epsg)
        {
            if(this.epsg != epsg) ReloadLocalSystem(epsg);
            return Project(meter, localSystem, globalSystem);
        }


        public Point[] ProjectToGlobalSystem(ICollection<Point> lonlats, int epsg)
        {
            if(this.epsg != epsg) ReloadLocalSystem(epsg);
            return Project(lonlats, localSystem, globalSystem);
        }


        private static Point Project(Point coord, ProjectionInfo fromSystem, ProjectionInfo toSystem)
        {
            double[] xy = { coord.X, coord.Y };
            double[] z = { 0 };
            Reproject.ReprojectPoints(xy, z, fromSystem, toSystem, 0, 1);
            return new Point(xy[0], xy[1]);
        }


        private static Point[] Project(ICollection<Point> coord, ProjectionInfo fromSystem, ProjectionInfo toSystem)
        {
            int count = coord.Count;
            if(count == 0) return Array.Empty<Point>();

            double[] xy = ArrayPool<double>.Shared.Rent(count * 2);
            double[] z = ArrayPool<double>.Shared.Rent(count);

            try
            {
                int index = 0;
                foreach(Point curCoord in coord)
                {
                    xy[index * 2 + 0] = curCoord.X;
                    xy[index * 2 + 1] = curCoord.Y;
                    z[index] = 0;
                    index++;
                }

                Reproject.ReprojectPoints(xy, z, fromSystem, toSystem, 0, count);

                Point[] result = new Point[count];
                for(int i = 0; i < result.Length; i++)
                    result[i] = new Point(xy[i * 2 + 0], xy[i * 2 + 1]);
                return result;
            }
            finally
            {
                ArrayPool<double>.Shared.Return(xy);
                ArrayPool<double>.Shared.Return(z);
            }
        }
    }
}
