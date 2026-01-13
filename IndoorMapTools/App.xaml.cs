using IndoorMapTools.Helper;
using IndoorMapTools.Services.Infrastructure.INI;
using IndoorMapTools.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Input;
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

        public List<CultureInfo> AvailableCultures { get; private set; }
        private readonly Dictionary<CultureInfo, Dictionary<string, string>> dicts = new Dictionary<CultureInfo, Dictionary<string, string>>();

        private CultureInfo culture;
        public CultureInfo Culture
        {
            get => culture ??= new CultureInfo(DEFAULT_CULTURE_CODE);
            set
            {
                if(value == null) return;
                if(!dicts.ContainsKey(value)) return;
                culture = value;
                CultureInfo.DefaultThreadCurrentCulture = value;
                CultureInfo.DefaultThreadCurrentUICulture = value;
                CultureInfo.CurrentCulture = value;
                CultureInfo.CurrentUICulture = value;
                var lang = XmlLanguage.GetLanguage(value.IetfLanguageTag);
                foreach(Window window in Windows) window.Language = lang;
                foreach(var kvString in dicts[value]) Resources["strings." + kvString.Key] = kvString.Value;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            //AppDomain.CurrentDomain.TypeResolve += CurrentDomain_TypeResolve;

            // Read INI
            string tilesSource = new INIReader(INI_PATH).ReadValue(INI_APP_OPEN_STREET_MAP, INI_KEY_TILE_SOURCE_URL);
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
            
            foreach((string name, string uri) in CURSOR_DEFS) // 커서 리소스 로드
            {
                using var st = GetResourceStream(new Uri(uri)).Stream;
                Resources[name] = new Cursor(st, true);
            }


            // 종속성 주입
            var services = new ServiceCollection(); 
            services.AddSingleton<Services.Application.FloorMapEditService>();
            services.AddSingleton<Services.Application.IMessageService, Services.Presentation.MessageBoxService>();
            services.AddSingleton<Services.Application.IProjectPersistenceService, Services.Infrastructure.ProtoBuf.ProtoBufService>();
            services.AddSingleton<Services.Application.IResourceStringService, Services.Presentation.ResourceStringService>();
            services.AddSingleton<Services.Infrastructure.IMPJ.IMPJImportService>();
            services.AddSingleton<Services.Infrastructure.IMPJ.IMPJExportService>();
            services.AddSingleton<Services.Presentation.BackgroundService>();
            services.AddSingleton<ViewModel.AnalysisFormVM>();
            services.AddSingleton<ViewModel.ExportProjectVM>();
            services.AddSingleton<ViewModel.FloorListItemVM>();
            services.AddSingleton<ViewModel.GlobalMapVM>();
            services.AddSingleton<ViewModel.IndoorMapVM>();
            services.AddSingleton<ViewModel.LandmarkTreeItemVM>();
            services.AddSingleton<ViewModel.MainWindowVM>();
            services.AddSingleton<ViewModel.MapImageEditVM>();
            services.AddSingleton<View.MainWindow>();

            var provider = services.BuildServiceProvider();
            var mainWindow = provider.GetRequiredService<View.MainWindow>();
            mainWindow.Show();
        }


        //private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        //{
        //    var requested = new AssemblyName(args.Name).Name;

        //    if(requested == "KAILOSMapTools")
        //    {
        //        Console.WriteLine("=== KAILOSMapTools requested ===");
        //        Console.WriteLine(args.Name);
        //        Console.WriteLine(new System.Diagnostics.StackTrace(true).ToString());
        //        Console.WriteLine("================================");

        //        return typeof(IndoorMapTools.Model.Project).Assembly;
        //    }

        //    return null;
        //}

        //private static Assembly CurrentDomain_TypeResolve(object sender, ResolveEventArgs args)
        //{
        //    Console.WriteLine($"[TypeResolve] {args.Name}");

        //    var name = args.Name;
        //    var comma = name.IndexOf(',');
        //    if(comma >= 0) name = name.Substring(0, comma);

        //    const string oldNs = "KAILOSMapTools.ViewModel.";
        //    const string newNs = "IndoorMapTools.Model.";

        //    if(name.StartsWith(oldNs, StringComparison.Ordinal))
        //    {
        //        var mapped = newNs + name.Substring(oldNs.Length);
        //        var asm = typeof(Project).Assembly;

        //        // 실제로 존재하는 타입인지 확인하고 있으면 해당 어셈블리 반환
        //        if(asm.GetType(mapped, throwOnError: false, ignoreCase: false) != null)
        //            return asm;
        //    }

        //    return null;
        //}
    }
}