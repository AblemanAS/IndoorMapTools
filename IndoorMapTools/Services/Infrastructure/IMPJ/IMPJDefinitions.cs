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

using System;
using System.Collections.Generic;

namespace IndoorMapTools.Services.Infrastructure.IMPJ
{
    internal static class IMPJDefinitions
    {
        public const string META_ATTR_FILE_NAME = "meta.json";
        public const string LANDMARKS_ATTR_FILE_NAME = "landmarks.json";
        public const string FLOOR_ATTR_FILE_NAME = "floor.json";
        public const string FLOOR_IMAGE_FILE_NAME = "image.png";
        public const string FLOOR_OGM_FILE_NAME = "ogm.png";

        public const string PROP_PROJECT_CRS = "epsg";
        public const string PROP_BUILDING_NAME = "name";
        public const string PROP_BUILDING_ADDRESS = "address";
        public const string PROP_BUILDING_CENTER = "lonlat";
        public const string PROP_BUILDING_OUTLINE = "outline";

        public const string PROP_GROUP_TYPE = "type";
        public const string PROP_GROUP_EXITS = "exits";
        public static readonly List<string> GROUP_TYPE_STRING = new List<string> { "VT_ST", "VT_EV", "VT_ES", "ENT", "STN" };

        public const string PROP_LANDMARK_PARENTFLOOR = "floor";
        public const string PROP_LANDMARK_ACCESSTYPE = "type";
        public const string PROP_LANDMARK_X = "x";
        public const string PROP_LANDMARK_Y = "y";
        public const string PROP_LANDMARK_DIRECTION = "dir";
        public const string PROP_LANDMARK_OUTLINE = "outline";

        public const string PROP_FLOOR_NAME = "name";
        public const string PROP_FLOOR_LEVEL = "level";
        public const string PROP_FLOOR_HEIGHT = "height";
        public const string PROP_FLOOR_OFFSET = "offset";

        public const string PROP_FLOOR_IMAGE = "image";
        public const string PROP_FLOOR_IMAGE_TYPE = "type";
        public const string PROP_FLOOR_IMAGE_WIDTH = "width";
        public const string PROP_FLOOR_IMAGE_HEIGHT = "height";
        public const string PROP_FLOOR_IMAGE_PPM = "ppm";
        public const string PROP_FLOOR_OGM = "ogm";
        public const string PROP_FLOOR_OGM_RES = "res";

        public const int IMPORT_DEFAULT_EPSG = 5179;
        public const string IMPORT_GROUP_PREFIX = "Group";
        public const int EXPORT_METER_PRECISION = 2;
        public const int EXPORT_LONLAT_PRECISION = 12;
        public const string EXPORT_FLOOR_IMAGE_TYPE = "image/png";
    }
}
