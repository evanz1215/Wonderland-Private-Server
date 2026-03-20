using Prism.Mvvm;

namespace WonderlandServerWpf.ViewModels
{
    public class UpdateViewModel : BindableBase
    {
        private string _applicationUpdatesInfo = "尚無更新資訊";
        public string ApplicationUpdatesInfo
        {
            get { return _applicationUpdatesInfo; }
            set { SetProperty(ref _applicationUpdatesInfo, value); }
        }

        private string _mapUpdatesInfo = "尚無地圖更新資訊";
        public string MapUpdatesInfo
        {
            get { return _mapUpdatesInfo; }
            set { SetProperty(ref _mapUpdatesInfo, value); }
        }
    }
}
