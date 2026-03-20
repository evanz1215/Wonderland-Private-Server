using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WloCharacterViewer.Automation;
using WloCharacterViewer.Models;
using WloCharacterViewer.Network;
using WloCharacterViewer.Network.Handlers;

#pragma warning disable CS8602
namespace WloCharacterViewer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private GameClient? _client;
        private GameSession? _session;
        private BotController? _bot;

        // Login fields
        private string _host = "127.0.0.1";
        public string Host { get => _host; set { _host = value; OnPropertyChanged(); } }

        private int _port = 6414;
        public int Port { get => _port; set { _port = value; OnPropertyChanged(); } }

        private string _username = "wl01234567";
        public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }

        private string _password = "123456";
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }

        private byte _selectedSlot = 1;
        public byte SelectedSlot { get => _selectedSlot; set { _selectedSlot = value; OnPropertyChanged(); } }

        private string _statusText = "未連線";
        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }

        // State flags
        private bool _isInGame;
        public bool IsInGame { get => _isInGame; set { _isInGame = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotInGame)); } }
        public bool IsNotInGame => !_isInGame;

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        // Packet log
        private string _rawPacketLog = "";
        public string RawPacketLog { get => _rawPacketLog; set { _rawPacketLog = value; OnPropertyChanged(); } }

        // Post-login ViewModels
        private DashboardViewModel? _dashboardVM;
        public DashboardViewModel? DashboardVM { get => _dashboardVM; set { _dashboardVM = value; OnPropertyChanged(); } }

        private ChatViewModel? _chatVM;
        public ChatViewModel? ChatVM { get => _chatVM; set { _chatVM = value; OnPropertyChanged(); } }

        private InventoryViewModel? _inventoryVM;
        public InventoryViewModel? InventoryVM { get => _inventoryVM; set { _inventoryVM = value; OnPropertyChanged(); } }

        private BattleBotViewModel? _battleBotVM;
        public BattleBotViewModel? BattleBotVM { get => _battleBotVM; set { _battleBotVM = value; OnPropertyChanged(); } }

        private NavigationViewModel? _navigationVM;
        public NavigationViewModel? NavigationVM { get => _navigationVM; set { _navigationVM = value; OnPropertyChanged(); } }

        // Commands
        public ICommand LoginCommand { get; }
        public ICommand DisconnectCommand { get; }

        public MainViewModel()
        {
            LoginCommand = new RelayCommand(async _ => await LoginAndEnterAsync(), _ => !IsLoading && IsNotInGame);
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => IsInGame);
        }

        private async Task LoginAndEnterAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                StatusText = "請輸入帳號和密碼";
                return;
            }

            if (Username.Length < 4 || Username.Length > 14 || Password.Length < 4 || Password.Length > 14)
            {
                StatusText = "帳號和密碼長度必須在 4~14 字元之間";
                return;
            }

            IsLoading = true;
            StatusText = "正在連線...";
            RawPacketLog = $"Log: {ClientLogger.LogPath}\n";

            try
            {
                // === Step 1: Connect ===
                _client?.Dispose();
                _client = new GameClient();
                _client.OnLog += msg => LogPacket(msg);
                await _client.ConnectAsync(Host, Port);
                LogPacket($"已連線到 {Host}:{Port}");

                var initPackets = await _client.ReceivePacketsAsync(2000);
                if (initPackets.Count > 0)
                    LogPacket($"收到 {initPackets.Count} 個初始封包");

                // === Step 2: Login ===
                StatusText = "正在登入...";
                _client.SendLogin(Username, Password);

                var packets = await _client.ReceivePacketsAsync(5000);
                LogPacket($"收到 {packets.Count} 個回應封包");

                bool loginSuccess = false;
                uint userId = 0;
                CharacterInfo? selectedChar = null;

                foreach (var pktBytes in packets)
                {
                    if (pktBytes.Length < 5) continue;

                    var reader = new PacketReader(pktBytes);
                    byte ac = reader.Unpack8();
                    byte? subAc = pktBytes.Length > 5 ? reader.Unpack8() : null;

                    if (ac == 63 && subAc == 2)
                    {
                        if (reader.Remaining >= 4)
                        {
                            userId = reader.Unpack32();
                            loginSuccess = true;
                            LogPacket($"登入成功, UserID={userId}");
                        }
                    }
                    else if (ac == 63 && subAc == 1)
                    {
                        var char1 = GameClient.ParseCharacterData(reader);
                        var char2 = GameClient.ParseCharacterData(reader);

                        if (SelectedSlot == 1 && char1 != null && !char1.IsEmpty)
                            selectedChar = char1;
                        else if (SelectedSlot == 2 && char2 != null && !char2.IsEmpty)
                            selectedChar = char2;
                        else if (char1 != null && !char1.IsEmpty)
                            selectedChar = char1;
                        else if (char2 != null && !char2.IsEmpty)
                            selectedChar = char2;
                    }
                    else if (ac == 1 && subAc == 6)
                    {
                        StatusText = "登入失敗：帳號或密碼錯誤";
                        return;
                    }
                    else if (ac == 0 && subAc == 19)
                    {
                        StatusText = "登入失敗：帳號已在線上";
                        return;
                    }
                    else if (ac == 0 && subAc == 17)
                    {
                        StatusText = "登入失敗：客戶端版本錯誤";
                        return;
                    }
                }

                if (!loginSuccess)
                {
                    if (!StatusText.Contains("失敗"))
                        StatusText = "登入失敗：未收到預期的伺服器回應";
                    return;
                }

                if (selectedChar == null)
                {
                    StatusText = $"角色 {SelectedSlot} 不存在";
                    return;
                }

                // === Step 3: Select Character & Enter Game ===
                StatusText = $"正在進入遊戲 [{selectedChar.Slot}] {selectedChar.Name}...";

                _session = new GameSession(_client);
                _session.UserId = userId;
                _session.OnLog += msg => LogPacket(msg);
                RegisterHandlers(_session);

                _client.SendCharacterSelect(selectedChar.Slot);

                var enterPackets = await _client.ReceivePacketsAsync(8000);
                LogPacket($"收到 {enterPackets.Count} 個初始化封包");

                foreach (var pkt in enterPackets)
                    _session.Dispatcher.Dispatch(pkt, _session);

                _session.Character.LoadFromCharacterInfo(selectedChar);
                _session.IsInGame = true;

                // === Step 4: Setup Bot & UI ===
                _bot = new BotController();
                _bot.Attach(_session);

                DashboardVM = new DashboardViewModel(_session, _bot);
                ChatVM = new ChatViewModel(_session);
                InventoryVM = new InventoryViewModel(_session);
                BattleBotVM = new BattleBotViewModel(_session, _bot);
                NavigationVM = new NavigationViewModel(_session, _bot);

                _session.StartReceiveLoop();

                IsInGame = true;
                StatusText = $"已進入遊戲 — {_session.Character.Name} Lv.{_session.Character.Level}";
            }
            catch (Exception ex)
            {
                StatusText = $"錯誤：{ex.Message}";
                LogPacket($"!!! 錯誤: {ex}");
                ClientLogger.Log($"[LoginAndEnter] EXCEPTION: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void RegisterHandlers(GameSession session)
        {
            session.RegisterHandler(5, 3, new StatsHandler53());
            session.RegisterHandler(8, null, new StatUpdateHandler());
            session.RegisterHandler(3, null, new CharacterDataHandler());
            session.RegisterHandler(23, 5, new InventoryBagHandler());
            session.RegisterHandler(23, 11, new InventoryEquipHandler());
            session.RegisterHandler(26, 4, new GoldHandler());
            session.RegisterHandler(2, 1, new ChatWhisperHandler());
            session.RegisterHandler(2, 2, new ChatLocalHandler());
            session.RegisterHandler(2, 3, new ChatPartyHandler());
            session.RegisterHandler(2, 5, new ChatWorldHandler());
            session.RegisterHandler(2, 6, new ChatSystemHandler());
            session.RegisterHandler(11, null, new BattleHandler());
            session.RegisterHandler(50, null, new BattleRoundHandler());
        }

        private void Disconnect()
        {
            _bot?.Detach();
            _bot = null;
            _session?.Dispose();
            _session = null;
            _client = null;

            IsInGame = false;

            DashboardVM = null;
            ChatVM = null;
            InventoryVM = null;
            BattleBotVM = null;
            NavigationVM = null;

            StatusText = "已斷線";
        }

        private void LogPacket(string msg)
        {
            RawPacketLog += msg + Environment.NewLine;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<object?, Task>? _asyncExecute;
        private readonly Action<object?>? _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Func<object?, Task> asyncExecute, Func<object?, bool>? canExecute = null)
        {
            _asyncExecute = asyncExecute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public async void Execute(object? parameter)
        {
            if (_asyncExecute != null)
                await _asyncExecute(parameter);
            else
                _execute?.Invoke(parameter);
        }
    }
}
