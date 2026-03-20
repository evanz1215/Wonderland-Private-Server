using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Mvvm;
using WonderlandServerWpf.Services;

namespace WonderlandServerWpf.ViewModels
{
    public class OnlinePlayerEntry : BindableBase
    {
        public uint CharID { get; set; }
        public string CharName { get; set; }
        public string UserName { get; set; }
        public byte Level { get; set; }
        public int CurHP { get; set; }
        public int MaxHP { get; set; }
        public ushort MapID { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public string IP { get; set; }
        public string Status { get; set; }
        public uint Gold { get; set; }
        public int IM { get; set; }
        public int BonusPoints { get; set; }

        public string HPDisplay
        {
            get { return CurHP + " / " + MaxHP; }
        }

        public string PositionDisplay
        {
            get { return string.Format("({0}, {1})", X, Y); }
        }

        public string GoldDisplay
        {
            get { return Gold.ToString("N0"); }
        }
    }

    public class AllPlayerEntry : BindableBase
    {
        public uint CharID { get; set; }
        public string CharName { get; set; }
        public byte Level { get; set; }
        public ushort MapID { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public uint Gold { get; set; }
        public string Element { get; set; }
        public string Job { get; set; }
        public bool IsOnline { get; set; }

        public string PositionDisplay
        {
            get { return string.Format("({0}, {1})", X, Y); }
        }

        public string GoldDisplay
        {
            get { return Gold.ToString("N0"); }
        }

        public string StatusDisplay
        {
            get { return IsOnline ? "在線" : "離線"; }
        }
    }

    public class OnlineUsersViewModel : BindableBase
    {
        readonly IServerEngine _engine;
        readonly DispatcherTimer _refreshTimer;

        private ObservableCollection<OnlinePlayerEntry> _players = new ObservableCollection<OnlinePlayerEntry>();
        public ObservableCollection<OnlinePlayerEntry> Players
        {
            get { return _players; }
            set { SetProperty(ref _players, value); }
        }

        private OnlinePlayerEntry _selectedPlayer;
        public OnlinePlayerEntry SelectedPlayer
        {
            get { return _selectedPlayer; }
            set { SetProperty(ref _selectedPlayer, value); }
        }

        private string _searchText = "";
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                SetProperty(ref _searchText, value);
                RefreshList();
            }
        }

        private string _onlineCountText = "0 位玩家在線";
        public string OnlineCountText
        {
            get { return _onlineCountText; }
            set { SetProperty(ref _onlineCountText, value); }
        }

        private string _mapCountText = "0 張地圖";
        public string MapCountText
        {
            get { return _mapCountText; }
            set { SetProperty(ref _mapCountText, value); }
        }

        private string _lastRefreshText = "";
        public string LastRefreshText
        {
            get { return _lastRefreshText; }
            set { SetProperty(ref _lastRefreshText, value); }
        }

        // All Players tab
        private ObservableCollection<AllPlayerEntry> _allPlayers = new ObservableCollection<AllPlayerEntry>();
        public ObservableCollection<AllPlayerEntry> AllPlayers
        {
            get { return _allPlayers; }
            set { SetProperty(ref _allPlayers, value); }
        }

        private AllPlayerEntry _selectedAllPlayer;
        public AllPlayerEntry SelectedAllPlayer
        {
            get { return _selectedAllPlayer; }
            set { SetProperty(ref _selectedAllPlayer, value); }
        }

        private string _allPlayersSearchText = "";
        public string AllPlayersSearchText
        {
            get { return _allPlayersSearchText; }
            set
            {
                SetProperty(ref _allPlayersSearchText, value);
                FilterAllPlayers();
            }
        }

        private string _allPlayersCountText = "0 位角色";
        public string AllPlayersCountText
        {
            get { return _allPlayersCountText; }
            set { SetProperty(ref _allPlayersCountText, value); }
        }

        private string _allPlayersLastRefreshText = "";
        public string AllPlayersLastRefreshText
        {
            get { return _allPlayersLastRefreshText; }
            set { SetProperty(ref _allPlayersLastRefreshText, value); }
        }

        // Tab selection — auto-load when switching to "All Players"
        private int _selectedTabIndex = 0;
        private bool _allPlayersLoaded = false;
        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    if (value == 1 && !_allPlayersLoaded)
                    {
                        RefreshAllPlayers();
                    }
                }
            }
        }

        // Pagination
        private const int PageSize = 50;

        private int _currentPage = 1;
        public int CurrentPage
        {
            get { return _currentPage; }
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    ApplyPage();
                    RaisePropertyChanged(nameof(PageInfoText));
                }
            }
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get { return _totalPages; }
            set { SetProperty(ref _totalPages, value); }
        }

        public string PageInfoText
        {
            get { return "第 " + _currentPage + " / " + _totalPages + " 頁"; }
        }

        // Cache for filtering
        private System.Collections.Generic.List<AllPlayerEntry> _allPlayersCache = new System.Collections.Generic.List<AllPlayerEntry>();
        private System.Collections.Generic.List<AllPlayerEntry> _filteredCache = new System.Collections.Generic.List<AllPlayerEntry>();

        public DelegateCommand RefreshAllPlayersCommand { get; }
        public DelegateCommand PrevPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand FirstPageCommand { get; }
        public DelegateCommand LastPageCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand KickCommand { get; }
        public DelegateCommand AddGoldCommand { get; }
        public DelegateCommand AddIMCommand { get; }
        public DelegateCommand AddBonusCommand { get; }
        public DelegateCommand SetLevelCommand { get; }
        public DelegateCommand HealCommand { get; }
        public DelegateCommand TeleportCommand { get; }

        public OnlineUsersViewModel(IServerEngine engine)
        {
            _engine = engine;

            RefreshCommand = new DelegateCommand(RefreshList);
            RefreshAllPlayersCommand = new DelegateCommand(RefreshAllPlayers);
            PrevPageCommand = new DelegateCommand(() => { if (_currentPage > 1) CurrentPage--; });
            NextPageCommand = new DelegateCommand(() => { if (_currentPage < _totalPages) CurrentPage++; });
            FirstPageCommand = new DelegateCommand(() => { CurrentPage = 1; });
            LastPageCommand = new DelegateCommand(() => { CurrentPage = _totalPages; });
            KickCommand = new DelegateCommand(OnKick);
            AddGoldCommand = new DelegateCommand(OnAddGold);
            AddIMCommand = new DelegateCommand(OnAddIM);
            AddBonusCommand = new DelegateCommand(OnAddBonus);
            SetLevelCommand = new DelegateCommand(OnSetLevel);
            HealCommand = new DelegateCommand(OnHeal);
            TeleportCommand = new DelegateCommand(OnTeleport);

            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(5);
            _refreshTimer.Tick += (s, e) => RefreshList();
            _refreshTimer.Start();

            RefreshList();
        }

        void RefreshList()
        {
            try
            {
                var onlinePlayers = _engine.GetOnlinePlayers();
                string search = (_searchText ?? "").Trim().ToLowerInvariant();

                _players.Clear();
                foreach (var p in onlinePlayers)
                {
                    try
                    {
                        string charName = p.CharName ?? "";
                        string userName = p.UserAcc != null ? (p.UserAcc.UserName ?? "") : "";
                        string ip = "";
                        try { ip = p.SockAddress(); } catch { }

                        ushort mapId = 0;
                        try { if (p.CurMap != null) mapId = (ushort)p.CurMap.MapID; } catch { }
                        if (mapId == 0) mapId = p.LoginMap;

                        string status = "在線";
                        if (p.isDisconnected()) status = "離線中";

                        int imPoints = 0;
                        try { if (p.UserAcc != null) imPoints = p.UserAcc.IM; } catch { }

                        // Search filter
                        if (!string.IsNullOrEmpty(search))
                        {
                            if (!charName.ToLowerInvariant().Contains(search) &&
                                !userName.ToLowerInvariant().Contains(search) &&
                                !p.CharID.ToString().Contains(search) &&
                                !mapId.ToString().Contains(search))
                                continue;
                        }

                        _players.Add(new OnlinePlayerEntry
                        {
                            CharID = p.CharID,
                            CharName = charName,
                            UserName = userName,
                            Level = p.Level,
                            CurHP = p.CurHP,
                            MaxHP = p.FullHP,
                            MapID = mapId,
                            X = p.CurX,
                            Y = p.CurY,
                            IP = ip,
                            Status = status,
                            Gold = p.Gold,
                            IM = imPoints,
                            BonusPoints = 0
                        });
                    }
                    catch { }
                }

                OnlineCountText = _players.Count + " 位玩家在線";
                MapCountText = _engine.MapCount + " 張地圖";
                LastRefreshText = "更新於 " + DateTime.Now.ToString("HH:mm:ss");
            }
            catch { }
        }

        void RefreshAllPlayers()
        {
            try
            {
                var dt = _engine.GetAllCharacterSummaries();
                _allPlayersCache.Clear();

                // Build a set of online charIDs for status detection
                var onlineIds = new System.Collections.Generic.HashSet<uint>();
                try
                {
                    foreach (var p in _engine.GetOnlinePlayers())
                        onlineIds.Add(p.CharID);
                }
                catch { }

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        try
                        {
                            uint charID = uint.Parse(row["charID"].ToString());
                            long totalExp = 0;
                            long.TryParse(row["totalExp"].ToString(), out totalExp);

                            // Estimate level from totalExp (same formula as the game)
                            byte level = 1;
                            long accumulated = 0;
                            for (int lvl = 1; lvl < 200; lvl++)
                            {
                                accumulated += (int)Math.Round(Math.Pow(lvl + 1, 3.1) + 5);
                                if (totalExp < accumulated) { level = (byte)lvl; break; }
                                if (lvl == 199) level = 199;
                            }

                            _allPlayersCache.Add(new AllPlayerEntry
                            {
                                CharID = charID,
                                CharName = row["name"].ToString(),
                                Level = level,
                                MapID = ushort.Parse(row["location_map"].ToString()),
                                X = ushort.Parse(row["location_x"].ToString()),
                                Y = ushort.Parse(row["location_y"].ToString()),
                                Gold = uint.Parse(row["gold"].ToString()),
                                Element = ((Game.Affinity)byte.Parse(row["element"].ToString())).ToString(),
                                Job = row["job"].ToString() == "0" ? "-" : ((Game.RebornJob)byte.Parse(row["job"].ToString())).ToString(),
                                IsOnline = onlineIds.Contains(charID)
                            });
                        }
                        catch { }
                    }
                }

                _allPlayersLoaded = true;
                FilterAllPlayers();
                AllPlayersLastRefreshText = "更新於 " + DateTime.Now.ToString("HH:mm:ss");
            }
            catch { }
        }

        void FilterAllPlayers()
        {
            string search = (_allPlayersSearchText ?? "").Trim().ToLowerInvariant();
            _filteredCache.Clear();

            foreach (var entry in _allPlayersCache)
            {
                if (!string.IsNullOrEmpty(search))
                {
                    if (!(entry.CharName ?? "").ToLowerInvariant().Contains(search) &&
                        !entry.CharID.ToString().Contains(search) &&
                        !entry.MapID.ToString().Contains(search))
                        continue;
                }
                _filteredCache.Add(entry);
            }

            AllPlayersCountText = _filteredCache.Count + " 位角色";
            TotalPages = Math.Max(1, (int)Math.Ceiling(_filteredCache.Count / (double)PageSize));
            _currentPage = 1;
            RaisePropertyChanged(nameof(CurrentPage));
            RaisePropertyChanged(nameof(PageInfoText));
            ApplyPage();
        }

        void ApplyPage()
        {
            _allPlayers.Clear();
            int start = (_currentPage - 1) * PageSize;
            int end = Math.Min(start + PageSize, _filteredCache.Count);
            for (int i = start; i < end; i++)
                _allPlayers.Add(_filteredCache[i]);
        }

        Game.Player FindOnlinePlayer(uint charID)
        {
            var players = _engine.GetOnlinePlayers();
            foreach (var p in players)
                if (p.CharID == charID) return p;
            return null;
        }

        bool TryGetInput(string title, string label, string defaultValue, out int value)
        {
            value = 0;
            try
            {
                var dlg = new Views.InputDialog(title, label, defaultValue);
                if (Application.Current != null && Application.Current.MainWindow != null)
                    dlg.Owner = Application.Current.MainWindow;
                if (dlg.ShowDialog() == true)
                {
                    return int.TryParse(dlg.InputValue, out value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("無法開啟輸入視窗：" + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }

        void OnKick()
        {
            if (_selectedPlayer == null) return;
            uint charID = _selectedPlayer.CharID;
            string charName = _selectedPlayer.CharName ?? "";
            var result = MessageBox.Show(
                "確定要踢除玩家「" + charName + "」嗎？",
                "踢除玩家", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var p = FindOnlinePlayer(charID);
                if (p != null) p.Disconnect();
                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("踢除失敗：" + ex.ToString(), "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void OnAddGold()
        {
            if (_selectedPlayer == null) return;
            uint charID = _selectedPlayer.CharID;
            string charName = _selectedPlayer.CharName ?? "";
            int amount;
            if (!TryGetInput("增加金幣", "輸入金幣數量（負數為扣除）：", "10000", out amount)) return;

            try
            {
                var p = FindOnlinePlayer(charID);
                if (p == null) { MessageBox.Show("玩家已離線。"); return; }
                if (p.UserAcc == null) { MessageBox.Show("玩家帳號資料不可用。"); return; }

                p.AddGold(amount);
                p.SendGold();
                try { Server.System.GameLogger.LogGold(p, amount, "Admin UI"); } catch { }
                RefreshList();
                MessageBox.Show("已對「" + charName + "」" +
                    (amount >= 0 ? "增加" : "扣除") + " " + Math.Abs(amount) + " 金幣。",
                    "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作失敗：" + ex.ToString(), "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void OnAddIM()
        {
            if (_selectedPlayer == null) return;
            uint charID = _selectedPlayer.CharID;
            string charName = _selectedPlayer.CharName ?? "";
            int amount;
            if (!TryGetInput("增加商城點數", "輸入點數數量（負數為扣除）：", "1000", out amount)) return;

            try
            {
                var p = FindOnlinePlayer(charID);
                if (p == null) { MessageBox.Show("玩家已離線。"); return; }
                if (p.UserAcc == null) { MessageBox.Show("玩家帳號資料不可用。"); return; }

                p.UserAcc.IM += amount;
                if (p.UserAcc.IM < 0) p.UserAcc.IM = 0;
                int newIM = p.UserAcc.IM;

                // Save to database
                if (cGlobal.gUserDataBase != null)
                    cGlobal.gUserDataBase.UpdateUser(p.UserAcc.DataBaseID, im: newIM);

                // Send updated IM to client
                p.Send(Tools.FromFormat("bbdddd", 35, 4, newIM, 0, 0, 0));

                RefreshList();
                MessageBox.Show("已對「" + charName + "」設定 IM 為 " + newIM + "。",
                    "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作失敗：" + ex.ToString(), "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void OnAddBonus()
        {
            if (_selectedPlayer == null) return;
            MessageBox.Show("紅利點數功能尚未在後端實作。\n需要在資料庫新增欄位後才能使用。",
                "功能開發中", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void OnSetLevel()
        {
            if (_selectedPlayer == null) return;
            uint charID = _selectedPlayer.CharID;
            string charName = _selectedPlayer.CharName ?? "";
            byte curLevel = _selectedPlayer.Level;
            int level;
            if (!TryGetInput("設定等級", "輸入目標等級 (1-199)：", curLevel.ToString(), out level)) return;

            if (level < 1) level = 1;
            if (level > 199) level = 199;

            try
            {
                var p = FindOnlinePlayer(charID);
                if (p == null) { MessageBox.Show("玩家已離線。"); return; }

                // Calculate exp needed for this level
                long neededExp = 0;
                byte rebornFlag = (byte)(p.Reborn ? 1 : 0);
                for (int lvl = 1; lvl < level; lvl++)
                    neededExp += (int)Math.Round(Math.Pow(lvl + 1, (rebornFlag == 0) ? 3.1 : 3.3) + (rebornFlag == 0 ? 5 : 50));

                // TotalExp setter subtracts (Level * 6) internally, so compensate
                p.TotalExp = neededExp + level * 6;
                p.Send8_1(true);
                RefreshList();
                MessageBox.Show("已將「" + charName + "」等級設定為 " + level + "。",
                    "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作失敗：" + ex.ToString(), "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void OnHeal()
        {
            if (_selectedPlayer == null) return;
            uint charID = _selectedPlayer.CharID;
            string charName = _selectedPlayer.CharName ?? "";
            try
            {
                var p = FindOnlinePlayer(charID);
                if (p == null) { MessageBox.Show("玩家已離線。"); return; }

                p.FillHP();
                p.FillSP();
                p.Send8_1();
                RefreshList();
                MessageBox.Show("已完全恢復「" + charName + "」的 HP/SP。",
                    "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作失敗：" + ex.ToString(), "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void OnTeleport()
        {
            if (_selectedPlayer == null) return;
            MessageBox.Show("傳送功能開發中。", "功能開發中", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
