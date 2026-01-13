using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace IndoorMapTools.Services.Presentation
{
    /// <summary>
    /// 백그라운드 작업을 수행하고 진행 상황을 보고하는 기능을 제공 (UI 바인딩 가능)
    /// </summary>
    [ObservableObject]
    public partial class BackgroundService : BackgroundWorker
    {
        /// <summary> 태스크 이름 </summary>
        [ObservableProperty] private string taskName;

        /// <summary> 태스크 진행률 (0 - 100) </summary>
        [ObservableProperty] private int progressPercentage;

        /// <summary> Progress가 없는 Task가 실행되고 있는지 </summary>
        [ObservableProperty] private bool progressIndeterminated;

        /// <summary> 최근 태스크 작업 시간 </summary>
        public long EllapsedMilliseconds => stopwatch.ElapsedMilliseconds;

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

            // 후속 작업 배정
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
        /// <param name="taskName">태스크 이름</param>
        public void Run(Action execute, string taskName = "") => Run(execute, null, taskName);

        /// <summary>
        /// 주어진 작업 및 추가 작업을 이 객체의 작업으로 지정하여 
        /// 주 작업을 백그라운드에서 실행하고 완료 시 추가 작업을 실행합니다.
        /// </summary>
        /// <param name="execute">실행할 작업</param>
        /// <param name="executeOnCompleted">작업 완료 시 실행할 후속 작업 (UI 쓰레드에서 실행)</param>
        /// <param name="taskName">태스크 이름</param>
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
