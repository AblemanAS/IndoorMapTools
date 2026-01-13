using System;

namespace IndoorMapTools.Helper
{
    /// <summary>
    /// 정수형 진행률 보고를 위한, thread-safe 진행률 박스. 
    /// total 값에 따라 매번 백분율을 계산하여 정수로 cast하고, 
    /// 그 값이 변할 때마다 onProgressChanged를 실행. 
    /// UI 쓰레드 마샬링은 하지 않으므로, UI 반영을 위해서는 외부에서 Dispatcher 사용 필요.
    /// </summary>
    public class IntegerProgressBox : IProgress<int>
    {
        /// <summary> 진행률이 변경될 때 호출되는 액션 </summary>
        private readonly Action<int> onProgressChanged;
        private readonly object lockObject = new(); // 쓰레드 세이프를 위한 락 오브젝트
        private readonly int total;
        private int currentProgress; // 현재 진행률
        private int lastProgressPercentage; // 최근 진행률

        /// <summary>
        /// IntegerProgressBox 클래스의 새 인스턴스를 초기화
        /// </summary>
        /// <param name="onProgressChanged">진행률이 변경될 때 호출될 액션</param>
        public IntegerProgressBox(int total, Action<int> onProgressChanged)
        {
            this.total = total;
            this.onProgressChanged = onProgressChanged;
            lastProgressPercentage = 0;
        }

        /// <summary>
        /// IntegerProgressBox 클래스의 새 인스턴스를 초기화
        /// </summary>
        /// <param name="onProgressChanged">진행률이 변경될 때 호출될 액션</param>
        public IntegerProgressBox(Action<int> onProgressChanged) : this(100, onProgressChanged) { }

        /// <summary>
        /// 진행률을 초기화
        /// </summary>
        public void Reset()
        {
            lock(lockObject)
            {
                lastProgressPercentage = currentProgress = 0;
            }
        }

        /// <summary>
        /// 진행률을 보고
        /// </summary>
        /// <param name="progress">보고할 진행률 값</param>
        public void Report(int progress)
        {
            bool progressChanged = false;
            int currentProgressPercentage;

            lock(lockObject)
            {
                currentProgress = progress;
                currentProgressPercentage = 100 * currentProgress / total;
                if(currentProgressPercentage > lastProgressPercentage)
                {
                    lastProgressPercentage = currentProgressPercentage;
                    progressChanged = true;
                }
            }

            // 진행률이 변경되었을 때만 액션 호출
            if(progressChanged) onProgressChanged?.Invoke(currentProgress);
        }
    }
}
