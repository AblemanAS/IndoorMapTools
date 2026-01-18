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

using System;
using System.Windows.Shapes;
using System.Windows;

namespace FGAView.System.Windows.Controls.FGAVisuals
{
    public abstract class FGAShape : Shape
    {
        public abstract void CacheDefiningGeometry(Func<int, int, int, Rect> mappingFunction);

        private IFGALayoutMapper coordinator;

        protected override Size MeasureOverride(Size constraint) => default;
        protected override Size ArrangeOverride(Size finalSize)
        {
            coordinator = FGALayoutHelper.SearchLayoutMapper(this);
            if(coordinator != null) CacheDefiningGeometry(coordinator.GetItemLayoutRect);
            return base.ArrangeOverride(finalSize);
        }
    }
}
