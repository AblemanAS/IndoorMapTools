/********************************************************************************
Copyright 2026-present Korea Advanced Institute of Science and Technology (KAIST)

Author: Kyuho Son

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
********************************************************************************/

using IndoorMapTools.Services.Infrastructure.INI;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;

namespace IndoorMapTools
{
    public partial class App : Application
    {
        private const string INI_PATH = "config.ini";
        private const string INI_APP_OPEN_STREET_MAP = "OpenStreetMap";
        private const string INI_KEY_TILE_SOURCE_URL = "tile_src_url";
        private const string RESOURCE_TILE_SOURCE_URL = "TileSourceURL";

        private readonly Tuple<string, string>[] CURSOR_DEFS =
        {
            Tuple.Create("StairCursor", "pack://application:,,,/Resources/stair.cur"),
            Tuple.Create("ElevatorCursor", "pack://application:,,,/Resources/elevator.cur"),
            Tuple.Create("EscalatorCursor", "pack://application:,,,/Resources/escalator.cur"),
            Tuple.Create("EntranceCursor", "pack://application:,,,/Resources/entrance.cur"),
            Tuple.Create("StationCursor", "pack://application:,,,/Resources/station.cur"),
            Tuple.Create("MarkCursor", "pack://application:,,,/Resources/mark.cur"),
            Tuple.Create("UnmarkCursor", "pack://application:,,,/Resources/unmark.cur")
        }; // 커서 리소스 정의 (Resource Key, Path)


        protected override void OnStartup(StartupEventArgs e)
        {
            // Read INI
            try
            {
                foreach(var setting in ReadTileSourceSettings(new INIService(INI_PATH)))
                    Resources[setting.Key] = setting.Value;
            }
            catch(FormatException ex)
            {
                MessageBox.Show(ex.Message + "\nOpenStreetMap will be used.", "Tile source configuration",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // 커서 리소스 로드
            foreach((string name, string uri) in CURSOR_DEFS)
            {
                using var st = GetResourceStream(new Uri(uri)).Stream;
                Resources[name] = new Cursor(st, true);
            }

            var mainWindow = DependencyInjector.ServiceProvider.GetRequiredService<View.MainWindow>();
            mainWindow.Show();
        }

        private static Dictionary<string, object> ReadTileSourceSettings(INIService ini)
        {
            string Read(string key) => ini.ReadValue(INI_APP_OPEN_STREET_MAP, key);
            FormatException Invalid(string key, string reason) =>
                new FormatException("[OpenStreetMap] " + key + ": " + reason);
            int ReadInt(string key, int fallback)
            {
                string value = Read(key);
                if(value == null) return fallback;
                if(!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                    throw Invalid(key, "an integer is required.");
                return parsed;
            }

            var settings = new Dictionary<string, object>();
            string enabledText = Read("alternative_tile_source_enabled");
            if(enabledText == null) return settings;
            if(!bool.TryParse(enabledText, out bool enabled))
                throw Invalid("alternative_tile_source_enabled", "true or false is required.");
            if(!enabled) return settings;

            string url = Read(INI_KEY_TILE_SOURCE_URL);
            if(string.IsNullOrWhiteSpace(url)) throw Invalid(INI_KEY_TILE_SOURCE_URL, "a URL is required.");
            string probe = url;
            foreach(string token in new[] { "{x}", "{y}", "{z}", "{lon}", "{lat}" })
                probe = probe.Replace(token, "0");
            if(probe.Contains("{") || probe.Contains("}") || probe.Contains("\r") || probe.Contains("\n") ||
                !Uri.TryCreate(probe, UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw Invalid(INI_KEY_TILE_SOURCE_URL, "an HTTP(S) URL with supported placeholders is required.");

            int offset = ReadInt("tile_zoom_offset", 0);
            int minZoom = ReadInt("tile_min_zoom", 0);
            int maxZoom = ReadInt("tile_max_zoom", 19);
            if(minZoom < 0) throw Invalid("tile_min_zoom", "must be nonnegative.");
            if(maxZoom < minZoom) throw Invalid("tile_max_zoom", "must be at least tile_min_zoom.");
            if((long)maxZoom - offset < 0 || (long)minZoom - offset > 62)
                throw Invalid("tile_zoom_offset", "the source range must overlap supported XYZ zoom levels (0-62).");

            int count = ReadInt("tile_header_count", 0);
            if(count < 0) throw Invalid("tile_header_count", "must be nonnegative.");
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var headerProbe = new HttpRequestMessage();
            for(int i = 0; i < count; i++)
            {
                string key = "tile_header_" + (i + 1).ToString(CultureInfo.InvariantCulture);
                string line = Read(key);
                int colon = line?.IndexOf(':') ?? -1;
                if(colon <= 0 || line.Contains("\r") || line.Contains("\n"))
                    throw Invalid(key, "a single-line name:value header is required.");
                string name = line.Substring(0, colon).Trim();
                string value = line.Substring(colon + 1).Trim();
                if(headers.ContainsKey(name)) throw Invalid(key, "duplicate header name.");
                try { headerProbe.Headers.Add(name, value); }
                catch(Exception ex) when(ex is FormatException || ex is InvalidOperationException || ex is ArgumentException)
                { throw Invalid(key, "invalid request header."); }
                headers.Add(name, value);
            }

            settings[RESOURCE_TILE_SOURCE_URL] = url;
            settings["TileSourceHeaders"] = new ReadOnlyDictionary<string, string>(headers);
            settings["TileZoomOffset"] = offset;
            settings["TileMinZoom"] = minZoom;
            settings["TileMaxZoom"] = maxZoom;
            return settings;
        }
    }
}
