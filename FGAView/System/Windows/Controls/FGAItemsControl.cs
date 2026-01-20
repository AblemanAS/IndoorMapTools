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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FGAView.System.Windows.Controls
{
    /// <summary>
    /// ItemsPanel이 FGACanvas인 Selector
    /// </summary>
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(FGAItem))]
    public class FGAItemsControl : Selector
    {
        protected override bool IsItemItsOwnContainerOverride(object item) => item is FGAItem;
        protected override DependencyObject GetContainerForItemOverride() => new FGAItem();

        public FGAItemsControl()
        {
            ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(FGACanvas)));
            IsTabStop = false;
        }
    }
}
