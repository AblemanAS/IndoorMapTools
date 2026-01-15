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

        private Project ParentProject => Model.ParentGroup.ParentBuilding.ParentProject;

        [RelayCommand] public void Join(LandmarkGroup group) => Model.ParentGroup.ParentBuilding.MoveLandmarkToGroup(Model, group);
        [RelayCommand] private void CopyToFloors() => EntityOrganizer.CopyLandmarkToEveryFloors(Model, ParentProject.Namespace);
        [RelayCommand] private void Isolate() => EntityOrganizer.IsolateLandmark(Model, ParentProject.Namespace);
    }
}
