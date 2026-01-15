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
using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.Model
{
    public partial class Project : ObservableObject
    {
        // 서비스
        public Dictionary<string, int> Namespace { get; } = new();

        // 에디터 영역
        [ObservableProperty] private Building building;

        // 분석 영역 비주얼
        [ObservableProperty] private List<BitmapImage> reachableClusters;

        // Export 옵션
        [ObservableProperty] private int _CRS = 0;
        [ObservableProperty] private double reachableResolution = 0.25;
        [ObservableProperty] private bool conservativeCellValidation = true;
        [ObservableProperty] private bool directedReachableCluster = true;

        internal Project() => Building = new Building(this);
    }
}
