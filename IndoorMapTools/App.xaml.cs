using IndoorMapTools.Core;
using IndoorMapTools.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Markup;

namespace IndoorMapTools
{
    public partial class App : Application
    {
        private const string LOCAL_STRINGS_PATH = "strings.json";
        private const string DEFAULT_STRINGS_PATH = "pack://application:,,,/Resources/strings_default.json";
        private const string DEFAULT_CULTURE_CODE = "en-US";

        private const string INI_PATH = "config.ini";
        private const string INI_APP_OPEN_STREET_MAP = "OpenStreetMap";
        private const string INI_KEY_TILE_SOURCE_URL = "tile_src_url";

        private const string RESOURCE_TILE_SOURCE_URL = "TileSourceURL";

        private readonly Tuple<string, string, double, double, double>[] WIN32_CURSOR_DEFS =
        {
            Tuple.Create("StairCursor", "pack://application:,,,/Resources/stair.png", 0.5, 0.5, 2.0),
            Tuple.Create("ElevatorCursor", "pack://application:,,,/Resources/elevator.png", 0.5, 0.5, 2.0),
            Tuple.Create("EscalatorCursor", "pack://application:,,,/Resources/escalator.png", 0.5, 0.5, 2.0),
            Tuple.Create("EntranceCursor", "pack://application:,,,/Resources/entrance.png", 0.5, 0.5, 2.0),
            Tuple.Create("StationCursor", "pack://application:,,,/Resources/station.png", 0.5, 0.5, 2.0),
            Tuple.Create("MarkCursor", "pack://application:,,,/Resources/mark.png", 0.3125, 0.3125, 1.0),
            Tuple.Create("UnmarkCursor", "pack://application:,,,/Resources/unmark.png", 0.3125, 0.3125, 1.0)
        }; // 커서 리소스 정의 (Resource Key, Path, X HotSpot, Y HotSpot, Scale)

        public List<CultureInfo> AvailableCultures { get; private set; }
        private readonly Dictionary<CultureInfo, Dictionary<string, string>> dicts = new Dictionary<CultureInfo, Dictionary<string, string>>();

        private CultureInfo culture;
        public CultureInfo Culture
        {
            get => culture ??= new CultureInfo(DEFAULT_CULTURE_CODE);
            set
            {
                if(!dicts.ContainsKey(value)) return;

                culture = value;
                CultureInfo.CurrentCulture = value;
                CultureInfo.CurrentUICulture = value;
                Thread.CurrentThread.CurrentCulture = value;
                Thread.CurrentThread.CurrentUICulture = value;

                foreach(Window window in Windows)
                    window.Language = XmlLanguage.GetLanguage(value.Name);

                foreach(var kvString in dicts[value])
                    Resources["strings." + kvString.Key] = kvString.Value;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            //new GPUAcc().Test(); // GPU Acceleration Test (ILGPU)

            // Read INI
            string tilesSource = new INIModule(INI_PATH).ReadValue(INI_APP_OPEN_STREET_MAP, INI_KEY_TILE_SOURCE_URL);
            if(tilesSource != null) Resources[RESOURCE_TILE_SOURCE_URL] = tilesSource;

            
            try // 기본 string resource (미국 영어) 로드
            {
                using StreamReader reader = new StreamReader(GetResourceStream(new Uri(DEFAULT_STRINGS_PATH)).Stream);
                var jsonDefaultDict = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(reader.ReadToEnd());
                dicts[new CultureInfo(DEFAULT_CULTURE_CODE)] = jsonDefaultDict;
            } catch(Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning); }

            try // Default (미국 영어) String Resources 로드
            {
                using StreamReader reader = new StreamReader(GetResourceStream(new Uri(DEFAULT_STRINGS_PATH)).Stream);
                var jsonDefaultDict = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(reader.ReadToEnd());
                dicts[new CultureInfo(DEFAULT_CULTURE_CODE)] = jsonDefaultDict;
            } catch(Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning); }

            try // Local String Resources 로드
            {
                var jsonLocalDict = new JavaScriptSerializer().Deserialize<Dictionary<string,
                    Dictionary<string, string>>>(File.ReadAllText(LOCAL_STRINGS_PATH));
                foreach(var kvLocalDict in jsonLocalDict)
                    dicts[new CultureInfo(kvLocalDict.Key)] = kvLocalDict.Value;
            } catch{}

            AvailableCultures = new List<CultureInfo>();
            foreach(var kvDict in dicts) AvailableCultures.Add(kvDict.Key);

            // Culture 초기화
            var systemCulture = CultureInfo.CurrentCulture;
            Culture = new CultureInfo(DEFAULT_CULTURE_CODE); // 기본 문자열 셋 로드 (영어, 미국)
            Culture = systemCulture; // 지역 문자열 셋 로드 (OS 시스템 언어)
            
            foreach(var def in WIN32_CURSOR_DEFS) // 커서 리소스 로드
                Resources[def.Item1] = Win32CursorLoader.Load(def.Item2, def.Item3, def.Item4, def.Item5);
            
            base.OnStartup(e);
        }
    }
}