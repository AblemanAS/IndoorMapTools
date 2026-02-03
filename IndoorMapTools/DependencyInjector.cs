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

using Microsoft.Extensions.DependencyInjection;
using System;

namespace IndoorMapTools
{
    public class DependencyInjector
    {
        public static ServiceProvider ServiceProvider { get => provider ??= Build(); }
        private static ServiceProvider provider;

        private static ServiceProvider Build() // 한 번만 호출돼야 함
        {
            var services = new ServiceCollection();
            services.AddSingleton<Services.Application.FloorMapEditService>();
            services.AddSingleton<Services.Application.IMessageService>(
                sp => sp.GetRequiredService<Services.Presentation.MessageBoxService>());
            services.AddSingleton<Services.Application.IProjectPersistenceService>(
                sp => sp.GetRequiredService<Services.Infrastructure.ProtoBuf.ProtoBufService>());
            services.AddSingleton<Services.Application.IResourceStringService>(
                sp => sp.GetRequiredService<Services.Presentation.LocalizationService>());

            services.AddSingleton<Services.Infrastructure.GeoLocation.GeoLocationService>();
            services.AddSingleton<Services.Infrastructure.IMPJ.IMPJImportService>();
            services.AddSingleton<Services.Infrastructure.IMPJ.IMPJExportService>();
            services.AddSingleton<Services.Infrastructure.ProtoBuf.ProtoBufService>();

            services.AddSingleton<Services.Presentation.BackgroundService>();
            services.AddSingleton<Services.Presentation.MessageBoxService>();
            services.AddSingleton<Services.Presentation.LocalizationService>();

            services.AddSingleton<ViewModel.AnalysisFormVM>();
            services.AddSingleton<ViewModel.ExportProjectVM>();
            services.AddSingleton<ViewModel.FloorListItemVM>();
            services.AddSingleton<ViewModel.GlobalMapVM>();
            services.AddSingleton<ViewModel.IndoorMapVM>();
            services.AddSingleton<ViewModel.LandmarkTreeItemVM>();
            services.AddSingleton<ViewModel.MainWindowVM>();
            services.AddSingleton<View.MainWindow>();

            return services.BuildServiceProvider();
        }
    }
}
