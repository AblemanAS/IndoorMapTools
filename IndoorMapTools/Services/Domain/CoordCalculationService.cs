using IndoorMapTools.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IndoorMapTools.Services.Domain
{
    public class CoordCalculationService
    {
        public static Point PixelCoordToMeterCoord(Point pixelCoord, int mapImagePixelHeight, double mapImagePPM)
        {
            if(mapImagePixelHeight < 1 || mapImagePPM > 1e+8) return new Point(Double.NaN, Double.NaN);
            return new Point(pixelCoord.X / mapImagePPM, (mapImagePixelHeight - pixelCoord.Y) / mapImagePPM);
        }
    }
}
