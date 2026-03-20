using System;
using Prism.Commands;
using Prism.Mvvm;

namespace WonderlandServerWpf.ViewModels
{
    public class AdminViewModel : BindableBase
    {
        // Database connection settings (Admin > Settings > Database > Config)
        private string _dbServerType = "";
        public string DbServerType
        {
            get { return _dbServerType; }
            set { SetProperty(ref _dbServerType, value); }
        }

        private string _dbServerIP = "";
        public string DbServerIP
        {
            get { return _dbServerIP; }
            set { SetProperty(ref _dbServerIP, value); }
        }

        private string _dbPort = "";
        public string DbPort
        {
            get { return _dbPort; }
            set { SetProperty(ref _dbPort, value); }
        }

        private string _dbUser = "";
        public string DbUser
        {
            get { return _dbUser; }
            set { SetProperty(ref _dbUser, value); }
        }

        private string _dbPass = "";
        public string DbPass
        {
            get { return _dbPass; }
            set { SetProperty(ref _dbPass, value); }
        }

        private string _dbDatabase = "";
        public string DbDatabase
        {
            get { return _dbDatabase; }
            set { SetProperty(ref _dbDatabase, value); }
        }

        public DelegateCommand SaveCommand { get; }

        public AdminViewModel()
        {
            SaveCommand = new DelegateCommand(OnSave);
            LoadSettings();
        }

        void LoadSettings()
        {
            if (cGlobal.SrvSettings != null && cGlobal.SrvSettings.DB != null)
            {
                DbServerIP = cGlobal.SrvSettings.DB.ServerIP ?? "";
                DbPort = cGlobal.SrvSettings.DB.Port.ToString();
                DbUser = cGlobal.SrvSettings.DB.User ?? "";
                DbPass = cGlobal.SrvSettings.DB.Pass ?? "";
                DbDatabase = cGlobal.SrvSettings.DB.DataBase ?? "";
            }
        }

        void OnSave()
        {
            // Save settings to XML
            try
            {
                string settingsDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + "\\PServer";
                if (!System.IO.Directory.Exists(settingsDir))
                    System.IO.Directory.CreateDirectory(settingsDir);

                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(Server.Config.Settings));
                using (var writer = new System.IO.StreamWriter(settingsDir + "\\Config.settings.wlo"))
                    serializer.Serialize(writer, cGlobal.SrvSettings);
            }
            catch { }
        }
    }
}
