using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Mvvm;
using WonderlandServerWpf.Services;

namespace WonderlandServerWpf.ViewModels
{
    public class TaskItemViewModel
    {
        public string TaskName { get; set; }
        public string Interval { get; set; }
        public string LastExecution { get; set; }
        public string NextExecution { get; set; }
        public string Status { get; set; }
    }

    public class SystemViewModel : BindableBase
    {
        readonly ILogService _log;
        readonly IServerEngine _engine;
        readonly DispatcherTimer _refreshTimer;

        private string _mainOutput = "";
        public string MainOutput
        {
            get { return _mainOutput; }
            set { SetProperty(ref _mainOutput, value); }
        }

        // Stat card values (numeric only for the big display)
        private string _playersOnlineCount = "0";
        public string PlayersOnlineCount
        {
            get { return _playersOnlineCount; }
            set { SetProperty(ref _playersOnlineCount, value); }
        }

        private string _playersIdleCount = "0";
        public string PlayersIdleCount
        {
            get { return _playersIdleCount; }
            set { SetProperty(ref _playersIdleCount, value); }
        }

        private string _mapsLoadedCount = "0";
        public string MapsLoadedCount
        {
            get { return _mapsLoadedCount; }
            set { SetProperty(ref _mapsLoadedCount, value); }
        }

        private string _mapsIdleCount = "0";
        public string MapsIdleCount
        {
            get { return _mapsIdleCount; }
            set { SetProperty(ref _mapsIdleCount, value); }
        }

        private string _threadCount = "0";
        public string ThreadCount
        {
            get { return _threadCount; }
            set { SetProperty(ref _threadCount, value); }
        }

        public ObservableCollection<TaskItemViewModel> Tasks { get; } = new ObservableCollection<TaskItemViewModel>();

        public DelegateCommand ShutdownCommand { get; }

        public SystemViewModel(ILogService log, IServerEngine engine)
        {
            _log = log;
            _engine = engine;

            ShutdownCommand = new DelegateCommand(OnShutdown);

            log.MainLogChanged += text =>
            {
                MainOutput = log.MainLogText;
            };

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _refreshTimer.Tick += (s, e) => RefreshStats();
            _refreshTimer.Start();
        }

        void RefreshStats()
        {
            try
            {
                ThreadCount = Process.GetCurrentProcess().Threads.Count.ToString();

                int online = _engine.OnlineCount;
                int maps = _engine.MapCount;
                PlayersOnlineCount = online.ToString();
                MapsLoadedCount = maps.ToString();

                // Idle counts not yet tracked by backend
                PlayersIdleCount = "0";
                MapsIdleCount = "0";
            }
            catch { }

            RefreshTasks();
        }

        void RefreshTasks()
        {
            if (cGlobal.ApplicationTasks == null) return;

            Tasks.Clear();
            try
            {
                foreach (var task in cGlobal.ApplicationTasks.TaskItems)
                {
                    Tasks.Add(new TaskItemViewModel
                    {
                        TaskName = task.TaskName,
                        Interval = task.Interval,
                        LastExecution = task.LastExecution,
                        NextExecution = task.NextExecution,
                        Status = task.Status
                    });
                }
            }
            catch { }
        }

        void OnShutdown()
        {
            _engine.Shutdown();
        }
    }
}
