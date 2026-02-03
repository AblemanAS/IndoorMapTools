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

using EnhancedCommands.Input;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace EnhancedCommands.Input.DialogCommands
{
    public abstract class DialogCommandBase : ContextCommand
    {
        [Bindable(true)]
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string),
            typeof(DialogCommandBase), new FrameworkPropertyMetadata(""));

        public override void Execute(object parameter) => Open();
        protected abstract void Open();
    }
}
