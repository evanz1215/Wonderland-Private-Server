using Prism.Mvvm;
using WonderlandServerWpf.Services;

namespace WonderlandServerWpf.ViewModels
{
    public class LogsViewModel : BindableBase
    {
        readonly ILogService _log;

        private string _systemLogText = "";
        public string SystemLogText
        {
            get { return _systemLogText; }
            set { SetProperty(ref _systemLogText, value); }
        }

        private string _networkLogText = "";
        public string NetworkLogText
        {
            get { return _networkLogText; }
            set { SetProperty(ref _networkLogText, value); }
        }

        private string _errorLogText = "";
        public string ErrorLogText
        {
            get { return _errorLogText; }
            set { SetProperty(ref _errorLogText, value); }
        }

        private int _selectedLogTab;
        public int SelectedLogTab
        {
            get { return _selectedLogTab; }
            set { SetProperty(ref _selectedLogTab, value); }
        }

        public LogsViewModel(ILogService log)
        {
            _log = log;

            log.SystemLogChanged += text => { SystemLogText = log.SystemLogText; };
            log.NetworkLogChanged += text => { NetworkLogText = log.NetworkLogText; };
            log.ErrorLogChanged += text => { ErrorLogText = log.ErrorLogText; };
        }
    }
}
