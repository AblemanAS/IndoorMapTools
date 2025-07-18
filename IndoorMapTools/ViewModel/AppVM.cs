using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Core;
using IndoorMapTools.ViewModel.Service;
using System;
using System.IO;
using System.Windows;

namespace IndoorMapTools.ViewModel
{
    public partial class AppVM : ObservableObject
    {
        // 공용 객체
        private static AppVM current;
        public static AppVM Current => current ??= new AppVM();

        // 서비스
        public BackgroundService BackgroundWorker { get; }

        // 에디터 영역
        [ObservableProperty] private Project project;

        public AppVM()
        {
            BackgroundWorker = new BackgroundService();
            BackgroundWorker.Run(() => GeoLocationModule.Initialize(), "Init PROJ Net");
        }

        public Point DefaultLonLat => new Point
        {
            X = double.Parse((string)Application.Current.Resources["strings.DEFAULT_LONGITUDE"]),
            Y = double.Parse((string)Application.Current.Resources["strings.DEFAULT_LATITUDE"])
        };

        // 앱 시스템 기능 핸들러
        [RelayCommand] private void NewProject() => Project = new Project();

        [RelayCommand] private void LoadProject(string filePath)
        {
            string extension = Path.GetExtension(filePath);

            switch(extension)
            {
                case ".impj":
                    BackgroundWorker.Run(() =>
                    {
                        Project = IMPJModule.Import(filePath, BackgroundWorker.ReportProgress);
                    }, (string)Application.Current.Resources["strings.ImportProjectStatusDesc"]);
                    break;

                case ".imtproj":
                    BackgroundWorker.Run(() =>
                    {
                        Project = BinaryModule.LoadProject(filePath);
                    }, (string)Application.Current.Resources["strings.LoadProjectStatusDesc"]);
                    break;
            }
        }
    }
}
