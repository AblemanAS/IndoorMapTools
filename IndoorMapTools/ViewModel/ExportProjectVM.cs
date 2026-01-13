using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Core;
using IndoorMapTools.Model;
using IndoorMapTools.Services.Application;
using IndoorMapTools.Services.Domain;
using IndoorMapTools.Services.Infrastructure.IMPJ;
using IndoorMapTools.Services.Presentation;
using System;
using System.ComponentModel;

namespace IndoorMapTools.ViewModel
{
    public partial class ExportProjectVM : ObservableObject
    {
        private readonly BackgroundService backgroundWorker;
        private readonly IMPJExportService impjExportSvc;
        private readonly IResourceStringService stringSvc;

        public ExportProjectVM(BackgroundService backgroundWorker, IResourceStringService stringSvc, IMPJExportService impjExportSvc)
        {
            this.backgroundWorker = backgroundWorker;
            this.stringSvc = stringSvc;
            this.impjExportSvc = impjExportSvc;
        }

        [ObservableProperty] private Project model;
        [ObservableProperty] private string _CRSName;
        [ObservableProperty] private string _CRSWKT;
        [ObservableProperty] private bool _CRSValid;
        [ObservableProperty] private bool areLandmarkOutlinesComplete;

        [NotifyCanExecuteChangedFor(nameof(ExportProjectCommand))]
        [ObservableProperty] private bool isLayoutValid;
        
        partial void OnModelChanged(Project oldModel, Project newModel)
        {
            void onCRSChanged(object sender, PropertyChangedEventArgs e)
            {
                CRSName = GeoLocationModule.ToName(Model.CRS);
                CRSWKT = GeoLocationModule.ToWKT(Model.CRS);
                bool crsValid = CRSName != null && CRSName.Length > 0;
                if(CRSValid != crsValid)
                {
                    CRSValid = crsValid;
                    IsLayoutValid = crsValid && AreLandmarkOutlinesComplete;
                }
            }

            if(oldModel != null) oldModel.PropertyChanged -= onCRSChanged;
            if(newModel != null)
            {
                newModel.PropertyChanged += onCRSChanged;
                onCRSChanged(null, null);
            }
        }

        [RelayCommand] private void InitDialogState()
        {
            AreLandmarkOutlinesComplete = EntityValidator.AreLandmarkOutlinesComplete(Model.Building.LandmarkGroups);
            IsLayoutValid = CRSValid && AreLandmarkOutlinesComplete;
        }

        [RelayCommand(CanExecute = nameof(IsLayoutValid))] private void ExportProject(string filePath)
            => backgroundWorker.Run(() => impjExportSvc.Export(Model, filePath, backgroundWorker.ReportProgress), 
                stringSvc["strings.ExportProjectStatusDesc"]);
    }
}
