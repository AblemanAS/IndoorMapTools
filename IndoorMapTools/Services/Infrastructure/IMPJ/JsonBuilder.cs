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

using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace IndoorMapTools.Services.Infrastructure.IMPJ
{
    /// <summary>
    /// JSOM 문자열 빌드 모듈 (문자열로 직렬화) 
    /// <para># 오브젝트 입력 : StartObject - 속성쌍들 입력 - EndObject</para>
    /// <para># 배열 입력 : StartList - 요소들 입력 - EndList</para>
    /// <para># 속성쌍들 입력 : WritePropertyPair - WritePropertyPair - ...</para>
    /// <para># 요소들 입력 : WriteValue - WriteValue - ...</para>
    /// <para>WritePropertyPair는 value로서 문자열, 실수, 정수, System.Windows.Point 및 그 열거형만을 지원하므로, 
    /// value 로서 다른 오브젝트를 추가하려면 WritePropertyName 후 # 오브젝트 입력 절차를 따라야 함 </para>
    /// </summary>
    internal class JsonBuilder
    {
        private enum StructureType { Object, List };

        private class JsonStructure
        {
            public StructureType Type;
            public bool LineFeed;
            public bool HasAnyElement = false;

            public JsonStructure(StructureType type, bool lineFeed) { Type = type; LineFeed = lineFeed; }
        }

        private readonly StringBuilder builder = new StringBuilder();
        private readonly Stack<JsonStructure> structureStack = new Stack<JsonStructure>();

        /// <summary>
        /// JSON 빌더와 구조 스택을 초기화 (문자열 출력 없이 객체 재활용 시에만 필요)
        /// </summary>
        public void Reset()
        {
            builder.Clear();
            structureStack.Clear();
            indentCount = 0;
            asValue = false;
        }

        /// <summary>
        /// JSON 문자열을 완성하여 출력하며 초기화
        /// </summary>
        /// <returns>완성된 JSON 문자열</returns>
        public string ToStringAndReset()
        {
            string result = builder.ToString();
            Reset();
            return result;
        }

        private int indentCount = 0;
        private bool asValue = false;

        /// <summary>
        /// JSON 객체를 시작 (이후 종료 필요)
        /// </summary>
        /// <param name="lineFeed">줄 바꿈 여부</param>
        public void StartObject(bool lineFeed = true)
        {
            PrepareAppend();
            builder.Append("{");
            structureStack.Push(new JsonStructure(StructureType.Object, lineFeed));
            if(lineFeed) indentCount++;
        }

        /// <summary>
        /// JSON 객체를 종료
        /// </summary>
        public void EndObject()
        {
            if(structureStack.Pop().LineFeed)
            {
                indentCount--;
                builder.Append("\n" + new string('\t', indentCount));
            }
            builder.Append("}");
        }

        /// <summary>
        /// JSON 배열을 시작 (이후 종료 필요)
        /// </summary>
        /// <param name="lineFeed">줄 바꿈 여부</param>
        public void StartList(bool lineFeed = true)
        {
            PrepareAppend();
            builder.Append("[");
            structureStack.Push(new JsonStructure(StructureType.List, lineFeed));
            if(lineFeed) indentCount++;
        }

        /// <summary>
        /// JSON 배열을 종료
        /// </summary>
        public void EndList()
        {
            if(structureStack.Pop().LineFeed)
            {
                indentCount--;
                builder.Append("\n" + new string('\t', indentCount));
            }
            builder.Append("]");
        }

        /// <summary>
        /// 속성 이름을 JSON에 추가 (이후 속성값을 추가하여 key:value pair 완결 필요)
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        public void WritePropertyName(string propertyName)
        {
            PrepareAppend();
            builder.Append("\"" + propertyName + "\": ");
            asValue = true;
        }

        /// <summary>
        /// 문자열 값을 JSON에 추가
        /// </summary>
        /// <param name="propertyValue">값</param>
        public void WriteValue(string propertyValue)
        {
            PrepareAppend();
            builder.Append("\"" + propertyValue + "\"");
        }

        /// <summary>
        /// 실수 값을 JSON에 추가
        /// </summary>
        /// <param name="propertyValue">값</param>
        /// <param name="precision">소수점 이하 자릿수</param>
        public void WriteValue(double propertyValue, int precision = 12)
        {
            string numExpr = "0." + new string('#', precision);
            PrepareAppend();
            builder.Append(propertyValue.ToString(numExpr));
        }

        /// <summary>
        /// 정수 값을 JSON에 추가
        /// </summary>
        /// <param name="propertyValue">값</param>
        public void WriteValue(int propertyValue)
        {
            PrepareAppend();
            builder.Append(propertyValue.ToString());
        }

        /// <summary>
        /// System.Windows.Point 값을 JSON에 추가
        /// </summary>
        /// <param name="propertyValue">값</param>
        /// <param name="precision">소수점 이하 자릿수</param>
        public void WriteValue(Point propertyValue, int precision = 12)
        {
            string numExpr = "0." + new string('#', precision);
            PrepareAppend();
            builder.Append("[" + propertyValue.X.ToString(numExpr) + ", " + propertyValue.Y.ToString(numExpr) + "]");
        }

        /// <summary>
        /// System.Windows.Point 열거 값을 한꺼번에 JSON에 추가
        /// </summary>
        /// <param name="propertyValue">값</param>
        public void WriteValue(IEnumerable<Point> propertyValue, int precision = 12)
        {
            string numExpr = "0." + new string('#', precision);
            PrepareAppend();
            builder.Append("[");
            var iter = propertyValue.GetEnumerator();
            if(iter.MoveNext()) builder.Append("[" + iter.Current.X.ToString(numExpr) + ", " + iter.Current.Y.ToString(numExpr) + "]");
            while(iter.MoveNext()) builder.Append(", [" + iter.Current.X.ToString(numExpr) + ", " + iter.Current.Y.ToString(numExpr) + "]");
            builder.Append("]");
        }

        /// <summary>
        /// 문자열 속성 쌍을 JSON에 추가 (시작/종료 통합 유형)
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        /// <param name="propertyValue">속성 값</param>
        public void WritePropertyPair(string propertyName, string propertyValue)
        {
            WritePropertyName(propertyName);
            WriteValue(propertyValue);
        }

        /// <summary>
        /// 실수 속성 쌍을 JSON에 추가 (시작/종료 내장 유형)
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        /// <param name="propertyValue">속성 값</param>
        /// <param name="precision">소수점 이하 자릿수</param>
        public void WritePropertyPair(string propertyName, double propertyValue, int precision = 12)
        {
            WritePropertyName(propertyName);
            WriteValue(propertyValue, precision);
        }

        /// <summary>
        /// 정수 속성 쌍을 JSON에 추가 (시작/종료 내장 유형)
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        /// <param name="propertyValue">속성 값</param>
        public void WritePropertyPair(string propertyName, int propertyValue)
        {
            WritePropertyName(propertyName);
            WriteValue(propertyValue);
        }

        /// <summary>
        /// System.Windows.Point 속성 쌍을 JSON에 추가 (시작/종료 내장 유형)
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        /// <param name="propertyValue">속성 값</param>
        /// <param name="precision">소수점 이하 자릿수</param>
        public void WritePropertyPair(string propertyName, Point propertyValue, int precision = 12)
        {
            WritePropertyName(propertyName);
            WriteValue(propertyValue, precision);
        }

        /// <summary>
        /// System.Windows.Point 열거 속성 쌍을 한꺼번에 JSON에 추가 (시작/종료 내장 유형)
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        /// <param name="propertyValue">속성 값</param>
        /// <param name="precision">소수점 이하 자릿수</param>
        public void WritePropertyPair(string propertyName, IEnumerable<Point> propertyValue, int precision = 12)
        {
            WritePropertyName(propertyName);
            WriteValue(propertyValue, precision);
        }


        private void PrepareAppend()
        {
            if(asValue) asValue = false;
            else if(structureStack.Count != 0)
            {
                var curStructure = structureStack.Peek();

                if(curStructure.HasAnyElement) builder.Append(", ");
                else curStructure.HasAnyElement = true;

                if(curStructure.LineFeed) builder.Append("\n" + new string('\t', indentCount));
            }
        }
    }
}




/*
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace IndoorMapTools.Core
{
    /// <summary>
    /// JSOM 문자열 빌드 모듈 (문자열로 직렬화) 
    /// <para># 오브젝트 입력 : StartObject - 속성쌍들 입력 - EndObject</para>
    /// <para># 배열 입력 : StartList - 요소들 입력 - EndList</para>
    /// <para># 속성쌍들 입력 : WritePropertyPair - WritePropertyPair - ...</para>
    /// <para># 요소들 입력 : WritePropertyPair - WritePropertyPair - ...</para>
    /// <para>WritePropertyPair는 value로서 문자열, 실수, 정수, Windows.Point, Windows.Point 열거형만을 지원하므로, 
    /// value 로서 다른 오브젝트를 추가하려면 WritePropertyName 후 # 오브젝트 입력 절차를 따라야 함 </para>
    /// 주의사항: 배열이나 객체와 같은 동등 계층의 복수 요소를 연달아 추가할 시, 
    /// 각 요소의 추가 사이사이에 반드시 호출 필요. 
    /// 
    /// </summary>
    public class JsonBuilder
    {
        private enum StructureType { Object, List };

        private class JsonStructure
        {
            public StructureType Type;
            public bool LineFeed;
            public bool HasAnyElement = false;

            public JsonStructure(StructureType type, bool lineFeed) { Type = type; LineFeed = lineFeed; }
        }

        private readonly StringBuilder builder = new StringBuilder();
        private readonly Stack<JsonStructure> structureStack = new Stack<JsonStructure>();

        /// <summary>
        /// JSON 빌더와 구조 스택을 초기화 (문자열 출력 없이 객체 재활용 시에만 필요)
        /// </summary>
        public void Reset()
        {
            builder.Clear();
            structureStack.Clear();
        }

        /// <summary>
        /// JSON 문자열을 완성하여 출력하며 초기화
        /// </summary>
        /// <returns>완성된 JSON 문자열</returns>
        public string ToStringAndReset()
        {
            string result = builder.ToString();
            Reset();
            return result;
        }

        private int indentCount = 0;
        private bool asValue = false;

        /// <summary>
        /// JSON 객체를 시작 (이후 종료 필요)
        /// </summary>
        /// <param name="lineFeed">줄 바꿈 여부</param>
        public void StartObject(bool lineFeed = true)
        {
            PrepareAppend();
            builder.Append("{");
            structureStack.Push(new JsonStructure(StructureType.Object, lineFeed));
            if(lineFeed) indentCount++;
        }

        /// <summary>
        /// JSON 객체를 종료
        /// </summary>
        public void EndObject()
        {
            if(structureStack.Pop().LineFeed)
            {
                indentCount--;
                builder.Append("\n" + new string('\t', indentCount));
            }
            builder.Append("}");
        }

        /// <summary>
        /// JSON 배열을 시작 (이후 종료 필요)
        /// </summary>
        /// <param name="lineFeed">줄 바꿈 여부</param>
        public void StartList(bool lineFeed = true)
        {
            PrepareAppend();
            builder.Append("[");
            structureStack.Push(new JsonStructure(StructureType.List, lineFeed));
            if(lineFeed) indentCount++;
        }

        /// <summary>
        /// JSON 배열을 종료
        /// </summary>
        public void EndList()
        {
            if(structureStack.Pop().LineFeed)
            {
                indentCount--;
                builder.Append("\n" + new string('\t', indentCount));
            }
            builder.Append("]");
        }

        /// <summary>
        /// 속성 이름을 JSON에 추가 (이후 속성값을 추가하여 key:value pair 완결 필요)
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        public void WritePropertyName(string propertyName)
        {
            PrepareAppend();
            builder.Append("\"" + propertyName + "\": ");
            asValue = true;
        }

        ///// <summary>
        ///// 문자열 속성 값을 JSON에 추가
        ///// </summary>
        ///// <param name="propertyValue">속성 값</param>
        //public void WriteValue(string propertyValue)
        //{
        //    PrepareAppend();
        //    builder.Append("\"" + propertyValue + "\"");
        //}

        /// <summary>
        /// 문자열 속성 쌍을 JSON에 추가 (시작/종료 내장 유형)
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        /// <param name="propertyValue">속성 값</param>
        public void WritePropertyPair(string propertyName, string propertyValue)
        {
            PrepareAppend();
            builder.Append("\"" + propertyName + "\": \"" + propertyValue + "\"");
        }

        /// <summary>
        /// 실수 속성 쌍을 JSON에 추가 (시작/종료 내장 유형)
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        /// <param name="propertyValue">속성 값</param>
        /// <param name="precision">소수점 이하 자릿수</param>
        public void WritePropertyPair(string propertyName, double propertyValue, int precision = 12)
        {
            string numExpr = "0." + new string('#', precision);
            PrepareAppend();
            builder.Append("\"" + propertyName + "\": " + propertyValue.ToString(numExpr));
        }

        /// <summary>
        /// 정수 속성 쌍을 JSON에 추가 (시작/종료 내장 유형)
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        /// <param name="propertyValue">속성 값</param>
        public void WritePropertyPair(string propertyName, int propertyValue)
        {
            PrepareAppend();
            builder.Append("\"" + propertyName + "\": " + propertyValue.ToString());
        }

        /// <summary>
        /// Point 속성 쌍을 JSON에 추가 (시작/종료 내장 유형)
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        /// <param name="propertyValue">속성 값</param>
        /// <param name="precision">소수점 이하 자릿수</param>
        public void WritePropertyPair(string propertyName, Point propertyValue, int precision = 12)
        {
            string numExpr = "0." + new string('#', precision);
            PrepareAppend();
            builder.Append("\"" + propertyName + "\": [" + propertyValue.X.ToString(numExpr) + ", " + propertyValue.Y.ToString(numExpr) + "]");
        }

        /// <summary>
        /// Point 열거 속성 쌍을 한꺼번에 JSON에 추가 (시작/종료 내장 유형)
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        /// <param name="propertyValue">속성 값</param>
        /// <param name="precision">소수점 이하 자릿수</param>
        public void WritePropertyPair(string propertyName, IEnumerable<Point> propertyValue, int precision = 12)
        {
            string numExpr = "0." + new string('#', precision);
            PrepareAppend();
            builder.Append("\"" + propertyName + "\": [");
            var iter = propertyValue.GetEnumerator();
            if(iter.MoveNext()) builder.Append("[" + iter.Current.X.ToString(numExpr) + ", " + iter.Current.Y.ToString(numExpr) + "]");
            while(iter.MoveNext()) builder.Append(", [" + iter.Current.X.ToString(numExpr) + ", " + iter.Current.Y.ToString(numExpr) + "]");
            builder.Append("]");
        }

        private void PrepareAppend()
        {
            if(asValue) asValue = false;
            else if(structureStack.Count != 0)
            {
                var curStructure = structureStack.Peek();

                if(curStructure.HasAnyElement) builder.Append(", ");
                else curStructure.HasAnyElement = true;

                if(curStructure.LineFeed) builder.Append("\n" + new string('\t', indentCount));
            }
        }
    }
}


 */