using System;
using System.ComponentModel;
using System.Diagnostics;

namespace IndoorMapTools.ViewModel.Service
{
    /// <summary>
    /// 백그라운드 작업을 수행하고 진행 상황을 보고하는 기능을 제공합니다.
    /// Context 모델로 구현되어, Observable Property를 가지므로 UI 바인딩이 가능합니다.
    /// </summary>
    public partial class BackgroundService : BackgroundWorker, INotifyPropertyChanged
    {
        private string taskName;
        /// <summary>태스크 이름</summary>
        public string TaskName
        { get => taskName; set { taskName = value; OnPropertyChanged(nameof(TaskName)); } }

        private int _progressPercentage;
        /// <summary>진행률 (최대 100)</summary>
        public int ProgressPercentage { get => _progressPercentage; set { _progressPercentage = value; OnPropertyChanged(nameof(ProgressPercentage)); } }

        private bool _progressIndeterminated;
        /// <summary>Progress가 없는 Task인지 여부</summary>
        public bool ProgressIndeterminated
        { get => _progressIndeterminated; set { _progressIndeterminated = value; OnPropertyChanged(nameof(ProgressIndeterminated)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public long EllapsedMilliseconds => stopwatch.ElapsedMilliseconds; // 최근 태스크 작업 시간

        private DoWorkEventHandler currentHandler;                      // 주 작업 핸들러
        private RunWorkerCompletedEventHandler currentCompletedHandler; // 추가 작업 핸들러
        private readonly Stopwatch stopwatch = new Stopwatch();         // 작업 시간 측정을 위한 스톱워치

        public BackgroundService()
        {
            WorkerReportsProgress = true;

            // 주 작업 배정
            ProgressChanged += (sender, e) =>
            {
                ProgressPercentage = e.ProgressPercentage;
                ProgressIndeterminated = false;
            };

            // 추가 작업 배정
            RunWorkerCompleted += (sender, e) =>
            {
                OnPropertyChanged(nameof(IsBusy));
                ProgressIndeterminated = false;
                ProgressPercentage = 0;
                TaskName = "";

                stopwatch.Stop();
                Debug.WriteLine("Background task time lapse : " + stopwatch.ElapsedMilliseconds + "ms");
            };
        }


        /// <summary>
        /// 주어진 작업을 이 객체의 작업으로 지정하고 백그라운드에서 실행합니다.
        /// </summary>
        /// <param name="execute">실행할 작업</param>
        /// <param name="taskName">태스크 이름 (선택적)</param>
        public void Run(Action execute, string taskName = "") => Run(execute, null, taskName);

        /// <summary>
        /// 주어진 작업 및 추가 작업을 이 객체의 작업으로 지정하여 
        /// 주 작업을 백그라운드에서 실행하고 완료 시 추가 작업을 실행합니다.
        /// </summary>
        /// <param name="execute">실행할 작업</param>
        /// <param name="executeOnCompleted">작업 완료 시 실행할 추가 작업 (선택적)</param>
        /// <param name="taskName">태스크 이름 (선택적)</param>
        public void Run(Action execute, Action executeOnCompleted, string taskName = "")
        {
            ClearTasks();

            if(execute == null) return;
            currentHandler = (sender, e) =>
            {
                execute();
                //try { execute(); }
                //catch (Exception ex)
                //{ MessageBox.Show($"{taskName}\n {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
            };
            DoWork += currentHandler;

            if(executeOnCompleted != null)
            {
                currentCompletedHandler = (sender, e) => executeOnCompleted();
                RunWorkerCompleted += currentCompletedHandler;
            }

            TaskName = taskName;
            ProgressIndeterminated = true;

            stopwatch.Start(); // 시간측정 시작

            RunWorkerAsync();
            OnPropertyChanged(nameof(IsBusy));
        }

        /// <summary>
        /// 현재 지정된 주 작업 및 추가 작업을 제거합니다.
        /// </summary>
        public void ClearTasks()
        {
            if(currentHandler != null) DoWork -= currentHandler;
            if(currentCompletedHandler != null) RunWorkerCompleted -= currentCompletedHandler;
        }
    }
}
