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

using System;
using System.Collections.Generic;
using System.Windows;

namespace FGAView.Controls
{
    public interface IFGALayoutMapper
    {
        public void UpdateReservation(UIElement item, IEnumerable<(int Floor, int Group, int Area)> identifiers);
        public Rect GetItemLayoutRect(int floor, int group, int area);
    }
}
