using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Core;
using IndoorMapTools.Model;
using IndoorMapTools.Services.Application;
using IndoorMapTools.Services.Domain;
using IndoorMapTools.Services.Infrastructure.IMPJ;
using IndoorMapTools.Services.Presentation;
using System;
using System.IO;

namespace IndoorMapTools.ViewModel
{
    public partial class MainWindowVM : ObservableObject
    {
        // 서비스
        private readonly BackgroundService backgroundSvc;
        private readonly IResourceStringService stringSvc;
        private readonly IProjectPersistenceService projectIOSvc;
        private readonly IMPJImportService impjImportSvc;

        // 하위 뷰모델
        public GlobalMapVM Gvm { get; }
        public IndoorMapVM Ivm { get; }
        public ExportProjectVM Evm { get; }
        public FloorListItemVM Fvm { get; }

        [ObservableProperty] private Project model; // 모델
        [ObservableProperty] private Floor selectedFloor;
        [ObservableProperty] private bool floorsVisible;

        public string TaskName => backgroundSvc.TaskName;
        public int ProgressPercentage => backgroundSvc.ProgressPercentage;
        public bool ProgressIndeterminated => backgroundSvc.ProgressIndeterminated;
        public bool IsBusy => backgroundSvc.IsBusy;

        private bool guardFloorSync = false;

        public MainWindowVM(GlobalMapVM gvm, IndoorMapVM ivm, ExportProjectVM evm, FloorListItemVM fvm,
                            BackgroundService backgroundSvc, IResourceStringService stringSvc, 
                            IProjectPersistenceService projectIOSvc, IMPJImportService impjImportSvc)
        {
            Gvm = gvm; Ivm = ivm; Evm = evm; Fvm = fvm;
            this.backgroundSvc = backgroundSvc;
            this.stringSvc = stringSvc;
            this.projectIOSvc = projectIOSvc;
            this.impjImportSvc = impjImportSvc;

            backgroundSvc.PropertyChanged += (sender, e) => 
            { if(!string.IsNullOrEmpty(e.PropertyName)) OnPropertyChanged(e.PropertyName); };

            backgroundSvc.Run(() => GeoLocationModule.Initialize(), "Init PROJ Net");

            Ivm.PropertyChanged += (sender, e) => // IVM의 모델 변경 감시 -> 자신의 층선택에 동기화
            {
                if(e.PropertyName == nameof(Ivm.Model) && SelectedFloor != Ivm.Model)
                    SelectedFloor = Ivm.Model;
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
            // 기존 모델 상태 구조 해제
            SelectedFloor = null;
            Gvm.Model = null;
            Ivm.Model = null;
            FloorsVisible = true;

            if(newModel == null) return;

            // 새로운 모델 상태 구조 구축
            Gvm.Model = newModel.Building;
            Evm.Model = newModel;
        }


        partial void OnSelectedFloorChanged(Floor newSelection)
        {
            if(guardFloorSync) return;
            guardFloorSync = true;
            try
            {
                if(Gvm.SelectedFloor != newSelection) Gvm.SelectedFloor = newSelection;
                if(Ivm.Model != newSelection) Ivm.Model = newSelection;
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
                ".impj" => () => Model = impjImportSvc.Import(filePath, backgroundSvc.ReportProgress),
                ".imtproj" => () => Model = projectIOSvc.LoadProject(filePath),
                ".kmtproj" => () => Model = projectIOSvc.LoadProject(filePath), // 호환성
                _ => null
            };

            if(loadBehavior == null) return;
            backgroundSvc.Run(loadBehavior, stringSvc["strings.LoadProjectStatusDesc"]);
        }

        [RelayCommand] private void SaveProject(string filePath)
            => backgroundSvc.Run(() => projectIOSvc.SaveProject(Model, filePath), stringSvc["strings.SaveProjectStatusDesc"]);

        [RelayCommand] private void SaveAndNewProject(string filePath)
        {
            SaveProject(filePath);
            NewProjectCommand.Execute(null);
        }

        // 층 리스트 핸들러
        [RelayCommand] private void CreateFloor(string filePath)
            => backgroundSvc.Run(() => Model?.Building.CreateFloor(ImageAlgorithms.BitmapImageFromFile(filePath), Gvm.GlobalMapFocus));

        [RelayCommand] private void BatchFloorName() => EntityNamer.BatchFloorName(Model.Building.Floors);
    }
}
