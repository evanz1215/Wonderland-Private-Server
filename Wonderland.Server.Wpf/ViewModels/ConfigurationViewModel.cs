using System;
using System.Collections.ObjectModel;
using Prism.Commands;
using Prism.Mvvm;

namespace WonderlandServerWpf.ViewModels
{
    public class ConfigurationViewModel : BindableBase
    {
        // Configuration > Database tab (User DataBase Table Column Linking)
        private string _tableName = "";
        public string TableName
        {
            get { return _tableName; }
            set { SetProperty(ref _tableName, value); }
        }

        private string _usernameRef = "";
        public string UsernameRef
        {
            get { return _usernameRef; }
            set { SetProperty(ref _usernameRef, value); }
        }

        private string _passwordRef = "";
        public string PasswordRef
        {
            get { return _passwordRef; }
            set { SetProperty(ref _passwordRef, value); }
        }

        private string _userIdRef = "";
        public string UserIdRef
        {
            get { return _userIdRef; }
            set { SetProperty(ref _userIdRef, value); }
        }

        private string _imRef = "";
        public string ImRef
        {
            get { return _imRef; }
            set { SetProperty(ref _imRef, value); }
        }

        private string _charDeleteCodeRef = "";
        public string CharDeleteCodeRef
        {
            get { return _charDeleteCodeRef; }
            set { SetProperty(ref _charDeleteCodeRef, value); }
        }

        private int _passVerification;
        public int PassVerification
        {
            get { return _passVerification; }
            set { SetProperty(ref _passVerification, value); }
        }

        public ObservableCollection<string> PassVerificationOptions { get; } = new ObservableCollection<string>
        {
            "None", "IP Board 3.x", "IP Board 4.x", "phpBB"
        };

        // Configuration > Update tab
        private bool _enableScheduledUpdate;
        public bool EnableScheduledUpdate
        {
            get { return _enableScheduledUpdate; }
            set
            {
                SetProperty(ref _enableScheduledUpdate, value);
                if (cGlobal.SrvSettings != null)
                    cGlobal.SrvSettings.Update.EnableSchedUpdate = value;
            }
        }

        private bool _warnOfUpdate;
        public bool WarnOfUpdate
        {
            get { return _warnOfUpdate; }
            set
            {
                SetProperty(ref _warnOfUpdate, value);
                if (cGlobal.SrvSettings != null)
                    cGlobal.SrvSettings.Update.WarnofUpdate = value;
            }
        }

        private int _selectedTab;
        public int SelectedTab
        {
            get { return _selectedTab; }
            set { SetProperty(ref _selectedTab, value); }
        }

        public ConfigurationViewModel()
        {
            LoadFromSettings();
        }

        void LoadFromSettings()
        {
            if (cGlobal.SrvSettings == null || cGlobal.SrvSettings.DB == null) return;

            TableName = cGlobal.SrvSettings.DB.TableName_Ref ?? "";
            UsernameRef = cGlobal.SrvSettings.DB.Username_Ref ?? "";
            PasswordRef = cGlobal.SrvSettings.DB.Password_Ref ?? "";
            UserIdRef = cGlobal.SrvSettings.DB.UserID_Ref ?? "";
            ImRef = cGlobal.SrvSettings.DB.IM_Ref ?? "";
            CharDeleteCodeRef = cGlobal.SrvSettings.DB.Char_Delete_Code_Ref ?? "";
            PassVerification = cGlobal.SrvSettings.DB.PassVerification;
            EnableScheduledUpdate = cGlobal.SrvSettings.Update != null && cGlobal.SrvSettings.Update.EnableSchedUpdate;
            WarnOfUpdate = cGlobal.SrvSettings.Update != null && cGlobal.SrvSettings.Update.WarnofUpdate;
        }
    }
}
