using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Core;
using IndoorMapTools.ViewModel.Service;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.ViewModel
{
    public partial class Project : ObservableObject
    {
        // 서비스
        public BackgroundService BackgroundWorker => AppVM.Current.BackgroundWorker;
        public Dictionary<string, int> Namespace { get; set; } = new Dictionary<string, int>();

        // 에디터 영역
        [ObservableProperty] private Building building;

        // 분석 영역 비주얼
        [ObservableProperty] private List<BitmapImage> reachableClusters;
        [ObservableProperty] private AnalysisReport report;

        // Export 옵션
        [ObservableProperty] private int _CRS = 0;
        [ObservableProperty] private double reachableResolution = 0.5;
        [ObservableProperty] private bool conservativeCellValidation = true;
        [ObservableProperty] private bool directedReachableCluster = true;

        public bool AreLandmarkOutlinesComplete
        {
            get
            {
                foreach(var group in Building.LandmarkGroups)
                    foreach(var landmark in group.Landmarks)
                        if(landmark.Outline.Length < 2) return false;
                return true;
            }
        }

        [RelayCommand] private void AnalyzeReachability()
        {
            Report = null; GC.Collect();
            BackgroundWorker.Run(() => Report = new AnalysisReport(Building, ReachableResolution,
                ConservativeCellValidation, DirectedReachableCluster, BackgroundWorker.ReportProgress),
              (string)Application.Current.Resources["strings.ReachableClusterAnalysisStatusDesc"]);
        }

        [RelayCommand] private void SaveProject(string filePath)
            => BackgroundWorker.Run(() => BinaryModule.SaveProject(this, filePath),
                (string)Application.Current.Resources["strings.SaveProjectStatusDesc"]);

        [RelayCommand] private void SaveAndNewProject(string filePath)
        {
            SaveProject(filePath);
            AppVM.Current.NewProjectCommand.Execute(null);
        }

        [RelayCommand] private void ExportProject(string filePath)
            => BackgroundWorker.Run(() => IMPJModule.Export(this, filePath, BackgroundWorker.ReportProgress),
                (string)Application.Current.Resources["strings.ExportProjectStatusDesc"]);

        public Project() => Building = new Building(this);

        public string GetNumberedName(string prefixName)
        {
            if(!Namespace.ContainsKey(prefixName))
            {
                Namespace[prefixName] = 1;
                return prefixName + 1;
            }

            return prefixName + ++Namespace[prefixName];
        }
    }
}
