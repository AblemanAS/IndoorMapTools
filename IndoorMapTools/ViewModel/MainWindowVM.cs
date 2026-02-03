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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Algorithm;
using IndoorMapTools.Model;
using IndoorMapTools.Services.Application;
using IndoorMapTools.Services.Domain;
using IndoorMapTools.Services.Infrastructure.IMPJ;
using IndoorMapTools.Services.Presentation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace IndoorMapTools.ViewModel
{
    public partial class MainWindowVM : ObservableObject
    {
        // 서비스
        private readonly BackgroundService bgSvc;
        private readonly LocalizationService strSvc;
        private readonly IProjectPersistenceService projectIOSvc;
        private readonly IMPJImportService impjImportSvc;

        // 하위 뷰모델
        public GlobalMapVM Gvm { get; }
        public IndoorMapVM Ivm { get; }
        public ExportProjectVM Evm { get; }
        public FloorListItemVM Fvm { get; }
        public AnalysisFormVM Avm { get; }

        [ObservableProperty] private Project model; // 모델
        [ObservableProperty] private Floor selectedFloor;
        [ObservableProperty] private bool floorsVisible;
        
        public string TaskName => bgSvc.TaskName;
        public int ProgressPercentage => bgSvc.ProgressPercentage;
        public bool ProgressIndeterminated => bgSvc.ProgressIndeterminated;
        public bool IsBusy => bgSvc.IsBusy;

        public IReadOnlyList<CultureInfo> AvailableCultures => strSvc.AvailableCultures;
        public CultureInfo CurrentCulture { get => strSvc.Culture; set => strSvc.Culture = value; }

        private bool guardFloorSync = false;

        public MainWindowVM(GlobalMapVM gvm, IndoorMapVM ivm, ExportProjectVM evm, FloorListItemVM fvm, AnalysisFormVM avm,
                            BackgroundService bgSvc, LocalizationService strSvc, 
                            IProjectPersistenceService projectIOSvc, IMPJImportService impjImportSvc)
        {
            Gvm = gvm; Ivm = ivm; Evm = evm; Fvm = fvm; Avm = avm;
            this.bgSvc = bgSvc;
            this.strSvc = strSvc;
            this.projectIOSvc = projectIOSvc;
            this.impjImportSvc = impjImportSvc;

            bgSvc.PropertyChanged += (sender, e) => 
            { if(!string.IsNullOrEmpty(e.PropertyName)) OnPropertyChanged(e.PropertyName); };

            strSvc.PropertyChanged += (sender, e) =>
            { if(!string.IsNullOrEmpty(e.PropertyName)) OnPropertyChanged(e.PropertyName); };

            Ivm.PropertyChanged += (sender, e) => // IVM의 모델 변경 감시 -> 자신의 층선택을 동기화
            {
                if(e.PropertyName == nameof(Ivm.MapViewModel) && SelectedFloor != Ivm.MapViewModel)
                    SelectedFloor = Ivm.MapViewModel;
            };

            Gvm.FloorStates.PropertyChanged += (_, e) =>
            {
                if(e.PropertyName != nameof(FloorState.Visible)) return;

                bool? unified = null;
                foreach(Floor curFloor in Model?.Building.Floors)
                {
                    FloorState curFloorState = Gvm.FloorStates[curFloor];
                    if(unified == null) unified = curFloorState.Visible;
                    else if(curFloorState.Visible != unified) return;
                }

                if(unified != null && FloorsVisible != unified)
                    FloorsVisible = (bool)unified;
            };
        }


        partial void OnModelChanged(Project oldModel, Project newModel)
        {
            // 기존 모델 상태 구조 해제, 속성 초기화
            Gvm.Model = null;
            Ivm.TreeViewModel = null;
            Ivm.MapViewModel = null;
            Evm.Model = null;
            Fvm.Model = null;
            Avm.Model = null;
            SelectedFloor = null;
            FloorsVisible = true;

            if(newModel == null) return;

            // 새로운 모델 상태 구조 구축
            Gvm.Model = newModel.Building;
            Ivm.TreeViewModel = newModel.Building;
            Evm.Model = newModel;
            Avm.Model = newModel;
        }


        partial void OnSelectedFloorChanged(Floor newSelection)
        {
            if(guardFloorSync) return;
            guardFloorSync = true;
            try
            {
                if(Gvm.SelectedFloor != newSelection) Gvm.SelectedFloor = newSelection;
                if(Ivm.MapViewModel != newSelection) Ivm.MapViewModel = newSelection;
                if(Fvm.Model != newSelection) Fvm.Model = newSelection;
            }
            finally { guardFloorSync = false; }
        }


        partial void OnFloorsVisibleChanged(bool value)
        {
            foreach(Floor curFloor in Model?.Building.Floors)
            {
                FloorState curFloorState = Gvm.FloorStates[curFloor];
                if(curFloorState.Visible != value)
                    curFloorState.Visible = value;
            }
        }
        

        // 앱 시스템 영역 핸들러
        [RelayCommand] private void NewProject() => Model = new Project();

        [RelayCommand] private void LoadProject(string filePath)
        {
            Action loadBehavior = Path.GetExtension(filePath) switch
            {
                ".impj" => () => Model = impjImportSvc.Import(filePath, bgSvc.ReportProgress),
                ".imtproj" => () => Model = projectIOSvc.LoadProject(filePath),
                ".kmtproj" => () => Model = projectIOSvc.LoadProject(filePath), // 호환성
                _ => null
            };

            if(loadBehavior == null) return;
            bgSvc.Run(loadBehavior, strSvc["LoadProjectStatusDesc"]);
        }

        [RelayCommand] private void SaveProject(string filePath)
            => bgSvc.Run(() => projectIOSvc.SaveProject(Model, filePath), strSvc["SaveProjectStatusDesc"]);

        [RelayCommand] private void SaveAndNewProject(string filePath)
        {
            SaveProject(filePath);
            NewProjectCommand.Execute(null);
        }

        // 층 리스트 핸들러
        [RelayCommand] private void CreateFloor(string filePath)
            => bgSvc.Run(() => Model?.Building.CreateFloor(EntityNamer.GetNumberedFloorName(Model.Namespace), 
                ImageAlgorithms.BitmapImageFromFile(filePath), Gvm.GlobalMapFocus));

        [RelayCommand] private void BatchFloorName() => EntityNamer.BatchFloorName(Model.Building.Floors);
    }
}
