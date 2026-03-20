using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Threading;

namespace WonderlandServerWpf.Services
{
    public interface ILogService
    {
        ObservableCollection<string> MainLog { get; }
        ObservableCollection<string> SystemLog { get; }
        ObservableCollection<string> NetworkLog { get; }
        ObservableCollection<string> ErrorLog { get; }

        string MainLogText { get; }
        string SystemLogText { get; }
        string NetworkLogText { get; }
        string ErrorLogText { get; }

        event Action<string> MainLogChanged;
        event Action<string> SystemLogChanged;
        event Action<string> NetworkLogChanged;
        event Action<string> ErrorLogChanged;

        void WriteMain(string text);
        void WriteSystem(string text);
        void WriteNetwork(string text);
        void WriteError(string text);
    }

    public class LogService : ILogService
    {
        readonly Dispatcher _dispatcher;
        readonly object _fileLock = new object();
        StreamWriter _logWriter;

        readonly StringBuilder _mainLogBuilder = new StringBuilder();
        readonly StringBuilder _systemLogBuilder = new StringBuilder();
        readonly StringBuilder _networkLogBuilder = new StringBuilder();
        readonly StringBuilder _errorLogBuilder = new StringBuilder();

        public ObservableCollection<string> MainLog { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> SystemLog { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> NetworkLog { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> ErrorLog { get; } = new ObservableCollection<string>();

        public string MainLogText { get { return _mainLogBuilder.ToString(); } }
        public string SystemLogText { get { return _systemLogBuilder.ToString(); } }
        public string NetworkLogText { get { return _networkLogBuilder.ToString(); } }
        public string ErrorLogText { get { return _errorLogBuilder.ToString(); } }

        public event Action<string> MainLogChanged;
        public event Action<string> SystemLogChanged;
        public event Action<string> NetworkLogChanged;
        public event Action<string> ErrorLogChanged;

        public LogService()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            InitFileLogger();
        }

        void InitFileLogger()
        {
            string logDir = "Logs";
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            string filename = Path.Combine(logDir, "server_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
            _logWriter = new StreamWriter(filename, true, Encoding.UTF8);
            _logWriter.AutoFlush = true;
        }

        void WriteToFile(string text)
        {
            if (_logWriter == null) return;
            lock (_fileLock)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    _logWriter.WriteLine("[" + timestamp + "] " + text);
                }
                catch { }
            }
        }

        public void WriteMain(string text)
        {
            WriteToFile(text);
            _mainLogBuilder.AppendLine(text);
            var handler = MainLogChanged;
            if (handler != null)
                _dispatcher.BeginInvoke(new Action(() => handler(text)));
        }

        public void WriteSystem(string text)
        {
            _systemLogBuilder.AppendLine(text);
            _systemLogBuilder.AppendLine("=============================");
            var handler = SystemLogChanged;
            if (handler != null)
                _dispatcher.BeginInvoke(new Action(() => handler(text)));
        }

        public void WriteNetwork(string text)
        {
            _networkLogBuilder.AppendLine(text);
            _networkLogBuilder.AppendLine("=============================");
            var handler = NetworkLogChanged;
            if (handler != null)
                _dispatcher.BeginInvoke(new Action(() => handler(text)));
        }

        public void WriteError(string text)
        {
            _errorLogBuilder.AppendLine(text);
            _errorLogBuilder.AppendLine("=============================");
            var handler = ErrorLogChanged;
            if (handler != null)
                _dispatcher.BeginInvoke(new Action(() => handler(text)));
        }
    }
}
