using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace IndoorMapTools.Core
{
    /// <summary>
    /// JSOM 문자열을 빌드하는 모듈입니다.
    /// </summary>
    public class JsonModule
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
        /// JSON 빌더와 구조 스택을 초기화합니다.
        /// </summary>
        public void Clear()
        {
            builder.Clear();
            structureStack.Clear();
        }

        /// <summary>
        /// JSON 문자열을 완성하고 초기화합니다.
        /// </summary>
        /// <returns>완성된 JSON 문자열</returns>
        public string Finalize()
        {
            string result = builder.ToString();
            Clear();
            return result;
        }

        private int indentCount = 0;
        private bool asValue = false;

        /// <summary>
        /// JSON 객체를 시작합니다.
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
        /// JSON 객체를 종료합니다.
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
        /// JSON 배열을 시작합니다.
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
        /// JSON 배열을 종료합니다.
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
        /// 다음 JSON 요소를 추가하기 위해 준비합니다.
        /// </summary>
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

        /// <summary>
        /// 문자열 속성 쌍을 JSON에 추가합니다.
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        /// <param name="propertyValue">속성 값</param>
        public void WritePropertyPair(string propertyName, string propertyValue)
        {
            PrepareAppend();
            builder.Append("\"" + propertyName + "\": \"" + propertyValue + "\"");
        }

        /// <summary>
        /// 실수 속성 쌍을 JSON에 추가합니다.
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
        /// 정수 속성 쌍을 JSON에 추가합니다.
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        /// <param name="propertyValue">속성 값</param>
        public void WritePropertyPair(string propertyName, int propertyValue)
        {
            PrepareAppend();
            builder.Append("\"" + propertyName + "\": " + propertyValue.ToString());
        }

        /// <summary>
        /// Point 속성 쌍을 JSON에 추가합니다.
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
        /// Point 배열 속성 쌍을 JSON에 추가합니다.
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

        /// <summary>
        /// 속성 이름을 JSON에 추가합니다.
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        public void WritePropertyName(string propertyName)
        {
            PrepareAppend();
            builder.Append("\"" + propertyName + "\": ");
            asValue = true;
        }
    }
}
