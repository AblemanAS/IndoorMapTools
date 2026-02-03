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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace IndoorMapTools.Services.Infrastructure.INI
{
    public class INIService
    {
        private const int BUFFER_SIZE = 65535;

        private readonly string path;

        public INIService(string path) => this.path = Path.Combine(AppContext.BaseDirectory, path);

        public string ReadValue(string appName, string keyName)
        {
            if(!File.Exists(path)) File.WriteAllText(path, "");
            var builder = new StringBuilder(BUFFER_SIZE);
            if(GetPrivateProfileString(appName, keyName, "", builder, (uint)builder.Capacity, path) > 0)
                return builder.ToString();
            return null;
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName,
            string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);

    }
}
