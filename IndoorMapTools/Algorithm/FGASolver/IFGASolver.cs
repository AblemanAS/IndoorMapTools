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

namespace IndoorMapTools.Algorithm.FGASolver
{
    // FGA 행렬 (locality를 위해서 FG가 아닌 G-row, F-col의 구조로 기록)
    // FGA Matrix는 Cell의 기본값이 Null이고, Area 값은 0으로 시작함
    // 이에 Null Cell과 Area 값 0 구분을 위해 Area 값에 +1 하여 대입
    // 따라서 값이 0인 경우는 Null Cell을 의미함
    public interface IFGASolver
    {
        /// <summary>
        /// FGA 행렬을 받아 FGA Problem을 풀고, 
        /// 재정렬 후 배치 기준 GroupId가 원래 배치 기준의 어느 GroupId의 Node들을 참조해야 하는지
        /// </summary>
        /// <param name="fgaMatrix">FGA 행렬을 명세대로 구성하여 전달 (Area 번호의 범위는 자연수로, 0이 부여된 Area는 null로 취급)</param>
        /// <returns>연산결과 인덱스 매핑 배열</returns>
        public int[] Solve(int[,] fgaMatrix);
    }
}