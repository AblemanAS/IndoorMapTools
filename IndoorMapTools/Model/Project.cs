using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.Model
{
    public partial class Project : ObservableObject
    {
        // 서비스
        public Dictionary<string, int> Namespace { get; set; } = new Dictionary<string, int>();

        // 에디터 영역
        [ObservableProperty] private Building building;

        // 분석 영역 비주얼
        [ObservableProperty] private List<BitmapImage> reachableClusters;
        [ObservableProperty] private AnalysisResult report;

        // Export 옵션
        [ObservableProperty] private int _CRS = 0;
        [ObservableProperty] private double reachableResolution = 0.25;
        [ObservableProperty] private bool conservativeCellValidation = true;
        [ObservableProperty] private bool directedReachableCluster = true;

        //[RelayCommand] private void AnalyzeReachability()
        //{
        //    Report = null; GC.Collect();
        //    backgroundWorker.Run(() => Report = new AnalysisReport(Building, ReachableResolution,
        //        ConservativeCellValidation, DirectedReachableCluster, backgroundWorker.ReportProgress),
        //      (string)Application.Current.Resources["strings.ReachableClusterAnalysisStatusDesc"]);
        //}

        internal Project() => Building = new Building(this);

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
