using Prism.Mvvm;
using WonderlandServerWpf.Services;

namespace WonderlandServerWpf.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _statusText = "就緒";
        public string StatusText
        {
            get { return _statusText; }
            set { SetProperty(ref _statusText, value); }
        }

        // The actual page index (0-5) used by the content area converter
        private int _activePageIndex;
        public int ActivePageIndex
        {
            get { return _activePageIndex; }
            set { SetProperty(ref _activePageIndex, value); }
        }

        // Top nav group (0=System, 1=Logs, 2=Online, 3=Admin) — maps to ActivePageIndex 0-3
        private int _selectedNavIndex;
        public int SelectedNavIndex
        {
            get { return _selectedNavIndex; }
            set
            {
                if (SetProperty(ref _selectedNavIndex, value) && value >= 0)
                {
                    _selectedSettingsIndex = -1;
                    RaisePropertyChanged("SelectedSettingsIndex");
                    ActivePageIndex = value;
                }
            }
        }

        // Bottom settings group (0=Config, 1=Update, 2=Mall) — maps to ActivePageIndex 4-6
        private int _selectedSettingsIndex = -1;
        public int SelectedSettingsIndex
        {
            get { return _selectedSettingsIndex; }
            set
            {
                if (SetProperty(ref _selectedSettingsIndex, value) && value >= 0)
                {
                    _selectedNavIndex = -1;
                    RaisePropertyChanged("SelectedNavIndex");
                    ActivePageIndex = value + 4;
                }
            }
        }

        public MainWindowViewModel(ILogService logService)
        {
            logService.MainLogChanged += msg =>
            {
                if (msg != null && msg.Contains("[Init]"))
                    StatusText = msg;
            };
        }
    }
}
