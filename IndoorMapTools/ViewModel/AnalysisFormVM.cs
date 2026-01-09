using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Model;
using System;

namespace IndoorMapTools.ViewModel
{
    public partial class AnalysisFormVM : ObservableObject
    {
        // 데이터 영역
        [ObservableProperty] private Landmark selectedLandmark;
        [ObservableProperty] private Floor selectedFloor;
        [ObservableProperty] private GraphNode selectedCluster;

        [RelayCommand] private void SelectLandmark(Landmark newval)
        {
            SelectedFloor = newval?.ParentFloor;
            SelectedLandmark = newval;
            SelectedCluster = null;
        }

        [RelayCommand] private void SelectCluster(GraphNode newval)
        {
            //SelectedLandmark = newval;
            //SelectedFloor = newval?.ParentFloor;
        }
    }
}
