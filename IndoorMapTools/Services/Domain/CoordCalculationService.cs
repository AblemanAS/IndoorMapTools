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
