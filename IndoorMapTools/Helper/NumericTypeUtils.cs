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

namespace IndoorMapTools.Helper
{
    /// <summary>
    /// 숫자 타입에 대한 유틸리티 클래스입니다.
    /// </summary>
    public static class NumericTypeUtils
    {
        /// <summary>
        /// 객체를 double 타입으로 인식합니다. (Native에서 Double로 변환 가능한 Numeric Type 및 string parse 지원)
        /// </summary>
        /// <param name="o">인식할 객체</param>
        /// <returns>변환된 double 값 또는 변환 실패 시 null</returns>
        public static object AsDouble(this object o)
        {
            try
            {
                return (double)Convert.ChangeType(o, TypeCode.Double);
            }
            catch
            {
                if(o is string str && double.TryParse(str, out double d)) return d;
                return null;
            }
        }

        /// <summary>
        /// 객체가 숫자 타입인지 확인합니다. (TypeCode Byte, SByte, UInt16, UInt32, UInt64, Int16, Int32, Int64, Decimal, Double, Single)
        /// </summary>
        /// <param name="o">확인할 객체</param>
        /// <returns>숫자 타입이면 true, 아니면 false</returns>
        public static bool IsNumericType(this object o)
        {
            switch(Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}
