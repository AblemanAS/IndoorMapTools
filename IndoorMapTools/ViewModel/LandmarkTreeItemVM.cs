using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Model;
using IndoorMapTools.Services.Domain;
using System;

namespace IndoorMapTools.ViewModel
{
    public partial class LandmarkTreeItemVM : ObservableObject
    {
        [ObservableProperty] private Landmark model;

        [RelayCommand] public void Join(LandmarkGroup group) => Model.ParentGroup.ParentBuilding.MoveLandmarkToGroup(Model, group);
        [RelayCommand] private void CopyToFloors() => EntityOrganizer.CopyLandmarkToEveryFloors(Model);
        [RelayCommand] private void Isolate() => EntityOrganizer.IsolateLandmark(Model);
    }
}
