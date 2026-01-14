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
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Media.Imaging;


namespace IndoorMapTools.OpenStreetMapControl
{
    public class TileCacheManager
    {
        private const string CACHE_PATH = "KAILOSMapToolsTileCache";
        private const int EXPIRATION_PERIOD = 7;

        public static TileCacheManager instance;
        public static TileCacheManager Instance => instance ??= new TileCacheManager();

        public TimeSpan ExpireAfter { get; set; } = TimeSpan.FromDays(EXPIRATION_PERIOD);
        private readonly string rootPath;

        public TileCacheManager()
        {
            rootPath = Path.Combine(Path.GetTempPath(), CACHE_PATH);
            CleanExpiredCache();
        }

        private string GetTilePath(long x, long y, int z) =>
            Path.Combine(rootPath, z.ToString(), x.ToString(), $"{y}.png");

        private string GetMetaPath(long x, long y, int z) =>
            GetTilePath(x, y, z) + ".meta";

        public bool TryLoadTile(long x, long y, int z, out BitmapImage image)
        {
            string tilePath = GetTilePath(x, y, z);
            string metaPath = GetMetaPath(x, y, z);
            image = null;

            if(!File.Exists(tilePath) || !File.Exists(metaPath))
                return false;

            try
            {
                string json = File.ReadAllText(metaPath);
                var meta = new JavaScriptSerializer().Deserialize<TileMeta>(json);
                if(DateTime.UtcNow - meta.DownloadedAt > ExpireAfter)
                    return false;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(tilePath);
                bmp.EndInit();
                bmp.Freeze();
                image = bmp;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SaveTile(long x, long y, int z, byte[] imageData)
        {
            string tilePath = GetTilePath(x, y, z);
            string metaPath = GetMetaPath(x, y, z);

            Directory.CreateDirectory(Path.GetDirectoryName(tilePath));
            File.WriteAllBytes(tilePath, imageData);
            SetTemporaryAttribute(tilePath);

            var meta = new TileMeta { DownloadedAt = DateTime.UtcNow };
            File.WriteAllText(metaPath, new JavaScriptSerializer().Serialize(meta));
            SetTemporaryAttribute(metaPath);
        }

        public void CleanExpiredCache()
        {
            if(!Directory.Exists(rootPath))
                return;

            foreach(var metaFile in Directory.GetFiles(rootPath, "*.meta", SearchOption.AllDirectories))
            {
                try
                {
                    string json = File.ReadAllText(metaFile);
                    var meta = new JavaScriptSerializer().Deserialize<TileMeta>(json);
                    if(DateTime.UtcNow - meta.DownloadedAt > ExpireAfter)
                    {
                        string tilePath = metaFile.Replace(".meta", "");
                        File.Delete(metaFile);
                        File.Delete(tilePath);
                    }
                }
                catch
                {
                    File.Delete(metaFile);
                }
            }
        }

        private class TileMeta
        {
            public DateTime DownloadedAt { get; set; }
        }

        // ===== Windows Temporary Attribute 설정 =====
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SetFileAttributes(string lpFileName, uint dwFileAttributes);

        private const uint FILE_ATTRIBUTE_TEMPORARY = 0x00000100;

        private void SetTemporaryAttribute(string filePath)
        { try { SetFileAttributes(filePath, FILE_ATTRIBUTE_TEMPORARY); } catch {} }
    }
}
