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
            string tilesSource = new INIService(INI_PATH).ReadValue(INI_APP_OPEN_STREET_MAP, INI_KEY_TILE_SOURCE_URL);
            if(tilesSource != null) Resources[RESOURCE_TILE_SOURCE_URL] = tilesSource;

            // 커서 리소스 로드
            foreach((string name, string uri) in CURSOR_DEFS)
            {
                using var st = GetResourceStream(new Uri(uri)).Stream;
                Resources[name] = new Cursor(st, true);
            }

            var mainWindow = DependencyInjector.ServiceProvider.GetRequiredService<View.MainWindow>();
            mainWindow.Show();
        }
    }
}