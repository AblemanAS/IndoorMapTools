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
using System.IO;
using System.IO.Compression;
using System.Text;

namespace IndoorMapTools.Services.Infrastructure.SRID
{
    internal class SRIDReader
    {
        private const int MAX_EPSG = 65535;

        public static (string Name, string WKT)[] ReadSRIDs(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                1024 * 1024, options: FileOptions.SequentialScan); // 1MB buffer
            using var archive = new ZipArchive(fs, ZipArchiveMode.Read);
            using var es = archive.Entries[0].Open();
            using var sr = new StreamReader(es, Encoding.UTF8, true, 1024 * 64, false); // 64KB buffer

            var result = new (string Name, string WKT)[MAX_EPSG];
            string line;
            while((line = sr.ReadLine()) is not null)
            {
                if(line.Length == 0) continue;
                string[] splitted = line.Split('|');
                if(int.Parse(splitted[0]) >= MAX_EPSG)
                {
                    continue;
                }
                result[int.Parse(splitted[0])] = (splitted[1], splitted[2]);
            }

            return result;
        }
    }
}
