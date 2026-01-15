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
using IndoorMapTools.Services.Application;
using IndoorMapTools.Services.Domain;
using IndoorMapTools.Services.Infrastructure.GeoLocation;
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
        private readonly IResourceStringService strSvc;
        private readonly GeoLocationService glSvc;

        public ExportProjectVM(BackgroundService backgroundWorker, 
            IResourceStringService strSvc, IMPJExportService impjExSvc, GeoLocationService glSvc)
        {
            this.backgroundWorker = backgroundWorker;
            this.strSvc = strSvc;
            this.impjExportSvc = impjExSvc;
            this.glSvc = glSvc;
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
                CRSName = glSvc.ToName(Model.CRS);
                CRSWKT = glSvc.ToWKT(Model.CRS);
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
                strSvc["strings.ExportProjectStatusDesc"]);
    }
}
