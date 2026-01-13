using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.SRID;
using System;
using System.Windows;

namespace IndoorMapTools.Core
{
    public static class GeoLocationModule
    {
        private static bool initialized = false;
        private static int epsg = 0;
        private static string name, wkt;
        private static CoordinateSystem globalSystem, localSystem;
        private static ICoordinateTransformation toLocal;
        private static ICoordinateTransformation toGlobal;

        public static void Initialize()
        {
            if(initialized) return;
            initialized = true;
            globalSystem = SRIDReader.GetCSbyID(4326);
        }

        public static string ToName(int epsg)
        {
            if(GeoLocationModule.epsg != epsg) ReloadLocalSystem(epsg);
            return name;
        }

        public static string ToWKT(int epsg)
        {
            if(GeoLocationModule.epsg != epsg) ReloadLocalSystem(epsg);
            return wkt;
        }

        /// <summary>
        /// EPSG 코드에 해당하는 지역 미터 좌표계를 다시 로드합니다.
        /// </summary>
        /// <param name="epsg">지역 미터 좌표계의 EPSG 코드</param>
        private static void ReloadLocalSystem(int epsg)
        {
            localSystem = SRIDReader.GetCSbyID(epsg);
            GeoLocationModule.epsg = epsg;

            if(localSystem == null)
            {
                name = wkt = "";
                return;
            }

            var ctFactory = new CoordinateTransformationFactory();
            toLocal = ctFactory.CreateFromCoordinateSystems(globalSystem, localSystem);
            toGlobal = ctFactory.CreateFromCoordinateSystems(localSystem, globalSystem);

            name = localSystem.Name;
            wkt = BeautifyWKT(localSystem.WKT);
        }

        /// <summary>
        /// WKT 문자열을 읽기 좋게 변환합니다.
        /// </summary>
        /// <param name="wkt">원본 kwt 문자열</param>
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

        public static Point ProjectToLocalSystem(Point lonlat, int epsg)
        {
            if(GeoLocationModule.epsg != epsg) ReloadLocalSystem(epsg);

            (double x, double y) = toLocal.MathTransform.Transform(lonlat.X, lonlat.Y);
            return new Point(x, y);
        }

        public static Point ProjectToGlobalSystem(Point meter, int epsg)
        {
            if(GeoLocationModule.epsg != epsg) ReloadLocalSystem(epsg);

            (double x, double y) = toGlobal.MathTransform.Transform(meter.X, meter.Y);
            return new Point(x, y);
        }

        /*
        private static int GetCRS(double lon, double lat)
        {
            int zone = (int)Math.Floor((lon + 180) / 6) + 1;

            if(lat >= 84) return 32661; // 북극
            else if(lat <= -80) return 32761; // 남극
            else if(lat >= 0) return 32600 + zone; // 북반구 
            else return 32700 + zone; // 남반구 
        }
        */

        private const double EarthRadius = 6378137.0; // WGS-84 기준 지구 반경 (meters)
        private const double TO_RAD_COEF = Math.PI / 180.0;
        private const double TO_DEG_COEF = 180.0 / Math.PI;

        public static Point TranslateWGSPoint(Point originLonLat, double xTranslationMeter, double yTranslationMeter)
        {
            double latRad = originLonLat.Y * TO_RAD_COEF;
            double lonRad = originLonLat.X * TO_RAD_COEF;

            double distance = Math.Sqrt(xTranslationMeter * xTranslationMeter + yTranslationMeter * yTranslationMeter);
            double bearing = Math.Atan2(xTranslationMeter, yTranslationMeter);

            double newLatRad = Math.Asin(Math.Sin(latRad) * Math.Cos(distance / EarthRadius) +
                                         Math.Cos(latRad) * Math.Sin(distance / EarthRadius) * Math.Cos(bearing));
            double newLonRad = lonRad + Math.Atan2(Math.Sin(bearing) * Math.Sin(distance / EarthRadius) * Math.Cos(latRad),
                                                   Math.Cos(distance / EarthRadius) - Math.Sin(latRad) * Math.Sin(newLatRad));

            return new Point(newLonRad * TO_DEG_COEF, newLatRad * TO_DEG_COEF);
        }


        public static Vector GetTranslationWGS(Point originLonLat, Point destinationLonLat)
        {
            double lat1Rad = originLonLat.Y * TO_RAD_COEF;
            double lat2Rad = destinationLonLat.Y * TO_RAD_COEF;

            double deltaLat = lat2Rad - lat1Rad;
            double deltaLon = (destinationLonLat.X - originLonLat.X) * TO_RAD_COEF;

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                       Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                       Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            double distance = EarthRadius * c;

            double y = Math.Sin(deltaLon) * Math.Cos(lat2Rad);
            double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) -
                       Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(deltaLon);
            double bearing = Math.Atan2(y, x);

            return new Vector(distance * Math.Sin(bearing), distance * Math.Cos(bearing));
        }
    }
}
