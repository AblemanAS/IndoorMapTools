using System;

namespace IndoorMapTools.Helper
{
    /// <summary>
    /// 정수형 진행률을 보고하며, 진행률이 변경될 때마다 액션을 호출하는 클래스 (thread-safe).
    /// UI 쓰레드 마샬링은 하지 않음. 별도 쓰레드에서 돌아가므로, UI 반영을 위해서는 외부에서 Dispatcher 사용 필요.
    /// </summary>
    public class IntegerProgressBox : IProgress<int>
    {
        /// <summary>
        /// 진행률이 변경될 때 호출되는 액션
        /// </summary>
        private readonly Action<int> onProgressChanged;
        private int lastProgress; // 최근 진행률
        private int currentProgress; // 현재 진행률
        private readonly object lockObject = new object(); // 쓰레드 세이프를 위한 락 오브젝트

        /// <summary>
        /// IntegerProgressBox 클래스의 새 인스턴스를 초기화
        /// </summary>
        /// <param name="onProgressChanged">진행률이 변경될 때 호출될 액션</param>
        public IntegerProgressBox(Action<int> onProgressChanged)
            => this.onProgressChanged = onProgressChanged;

        /// <summary>
        /// 진행률을 초기화
        /// </summary>
        public void Reset()
        {
            lock(lockObject)
            {
                lastProgress = currentProgress = 0;
            }
        }

        /// <summary>
        /// 진행률을 보고
        /// </summary>
        /// <param name="progress">보고할 진행률 값</param>
        public void Report(int progress)
        {
            bool progressChanged = false;

            lock(lockObject)
            {
                currentProgress = progress;
                if(currentProgress > lastProgress)
                {
                    lastProgress = currentProgress;
                    progressChanged = true;
                }
            }

            // 진행률이 변경되었을 때만 액션 호출
            if(progressChanged) onProgressChanged?.Invoke(currentProgress);
        }
    }
}
