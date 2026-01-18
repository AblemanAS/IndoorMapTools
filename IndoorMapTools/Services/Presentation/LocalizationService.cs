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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Markup;

namespace IndoorMapTools.Services.Presentation
{
    public class LocalizationService : Application.IResourceStringService, INotifyPropertyChanged
    {
        private const string DEFAULT_STRINGS_PATH = "pack://application:,,,/Resources/strings_default.json"; // 기본 언어 경로
        private const string DEFAULT_CULTURE_CODE = "en-US"; // 기본 언어
        private const string STRING_RESOURCE_PREFIX = "strings."; // 문자열 리소스 접두어
        private const string LOCAL_STRINGS_PATH = "strings.json"; // 추가 지역화 언어 경로

        public IReadOnlyList<CultureInfo> AvailableCultures { get; private set; }
        public CultureInfo Culture
        {
            get => culture;
            set
            {
                var app = System.Windows.Application.Current;
                Debug.Assert(app != null);
                if(app.Dispatcher.CheckAccess()) SetCulture(value);
                else app.Dispatcher.Invoke(() => SetCulture(value));
            }
        }

        public string Get(string key) => (string)System.Windows.Application.Current.Resources[STRING_RESOURCE_PREFIX + key];
        public string this[string key] => Get(key);
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly CultureInfo defaultCulture = new(DEFAULT_CULTURE_CODE);
        private CultureInfo culture;

        private void SetCulture(CultureInfo value)
        {
            try
            {
                CultureInfo resolved = ResolveCulture(value);
                CultureInfo.DefaultThreadCurrentCulture = resolved;
                CultureInfo.DefaultThreadCurrentUICulture = resolved;
                CultureInfo.CurrentCulture = resolved;
                CultureInfo.CurrentUICulture = resolved;
                var lang = XmlLanguage.GetLanguage(resolved.IetfLanguageTag);
                foreach(Window window in System.Windows.Application.Current.Windows) window.Language = lang;
                foreach(var kvString in dicts[resolved])
                    System.Windows.Application.Current.Resources[STRING_RESOURCE_PREFIX + kvString.Key] = kvString.Value;
                culture = resolved;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Culture)));
            }
            catch { SetCulture(null); } // fallback으로 기본 언어 설정
        }


        private CultureInfo ResolveCulture(CultureInfo ci)
        {
            if(ci == null) return defaultCulture;   // null일 경우 기본값 반환
            if(dicts.ContainsKey(ci)) return ci;    // dict에 바로 있으면 반환
            for(var curci = ci; !curci.Equals(CultureInfo.InvariantCulture); curci = curci.Parent)
                if(dicts.ContainsKey(curci)) return curci; // 부모 문화 타고 올라가면서 체크
            return defaultCulture;  // 모두 실패하면 그냥 기본값 반환
        }


        // CultureInfo : (str 리소스 key : value)
        // dict에서 CultureInfo 의 비교연산은 해시 기반이 아닌 문화권 내용 기반이므로 key로서 적합
        private readonly Dictionary<CultureInfo, Dictionary<string, string>> dicts = new();


        public LocalizationService()
        {
            // Load strings
            var jsonSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();

            // Default strings (기본 언어)
            var defaultStringsStream = System.Windows.Application.GetResourceStream(new Uri(DEFAULT_STRINGS_PATH)).Stream;
            using(var reader = new System.IO.StreamReader(defaultStringsStream, Encoding.UTF8))
                dicts[defaultCulture] = jsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadToEnd());

            // Localized strings
            if(System.IO.File.Exists(LOCAL_STRINGS_PATH))
            {
                try
                {
                    var localStringsText = System.IO.File.ReadAllText(LOCAL_STRINGS_PATH, Encoding.UTF8);
                    var localDicts = jsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(localStringsText);
                    foreach(var kvCulture in localDicts) // 전체 문자열 리소스 키에 대해 로드 시도
                    {
                        var curLocal = new CultureInfo(kvCulture.Key);
                        if(curLocal == defaultCulture) continue; // 기본 언어일 경우 덮어쓰지 않음
                        try { dicts[curLocal] = kvCulture.Value; } catch { } //  실패할 경우 그냥 버리기
                    }
                } catch { }
            }

            // 사용 가능 언어 뽑아 놓기
            AvailableCultures = new List<CultureInfo>(dicts.Keys);

            // 현재 언어 설정
            CultureInfo uiculture = CultureInfo.CurrentUICulture;   // 사용자 OS 언어코드 미리 받아 놓기
            Culture = defaultCulture;        // 안전 기본값 할당
            Culture = uiculture;                                    // 사용자 OS 언어코드 할당 시도
        }
    }
}
