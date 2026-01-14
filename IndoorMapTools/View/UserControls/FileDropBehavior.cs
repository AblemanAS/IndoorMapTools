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
using System.Windows;
using System.Windows.Input;

namespace IndoorMapTools.View.UserControls
{
    public static class FileDropBehavior
    {
        public static void SetFileDropCommand(DependencyObject d, ICommand value)
            => d.SetValue(FileDropCommandProperty, value);
        public static ICommand GetFileDropCommand(DependencyObject d)
            => (ICommand)d.GetValue(FileDropCommandProperty);
        public static readonly DependencyProperty FileDropCommandProperty = DependencyProperty.RegisterAttached(
                "FileDropCommand", typeof(ICommand), typeof(FileDropBehavior), new PropertyMetadata(null, OnChanged));

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is not UIElement ui) return;
            ui.AllowDrop = true;
            ui.Drop -= OnDrop;
            if(e.NewValue != null) ui.Drop += OnDrop;
        }


        private static void OnDrop(object sender, DragEventArgs e)
        {
            if(e.Data.GetData(DataFormats.FileDrop) is string[] filePaths)
            {
                var cmd = GetFileDropCommand((DependencyObject)sender);
                if(cmd != null && cmd.CanExecute(filePaths))
                    cmd.Execute(filePaths);
            }
        }

    }

}
