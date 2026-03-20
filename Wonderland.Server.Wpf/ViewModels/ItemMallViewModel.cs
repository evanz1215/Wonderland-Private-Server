using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using Server;
using WonderlandServerWpf.Services;

namespace WonderlandServerWpf.ViewModels
{
    /// <summary>
    /// Wrapper for display in the Transfer panels.
    /// </summary>
    public class MallItemEntry : BindableBase
    {
        public ushort ItemID { get; set; }
        public string Name { get; set; }
        public byte Tab { get; set; }
        public ushort Price { get; set; }
        public byte Amount { get; set; }
        public byte Discount { get; set; }

        private bool _isChecked;
        public bool IsChecked
        {
            get { return _isChecked; }
            set { SetProperty(ref _isChecked, value); }
        }

        public string DisplayText
        {
            get { return string.Format("[{0}] {1}", ItemID, Name); }
        }

        public string DisplayTextWithPrice
        {
            get { return string.Format("[{0}] {1}  (${2}, x{3})", ItemID, Name, Price, Amount); }
        }
    }

    public class ItemMallViewModel : BindableBase
    {
        readonly IServerEngine _engine;

        // ── Left Panel: Available items (from Item.dat, not yet in mall) ──
        private ObservableCollection<MallItemEntry> _availableItems = new ObservableCollection<MallItemEntry>();
        public ObservableCollection<MallItemEntry> AvailableItems
        {
            get { return _availableItems; }
            set { SetProperty(ref _availableItems, value); }
        }

        private ObservableCollection<MallItemEntry> _filteredAvailableItems = new ObservableCollection<MallItemEntry>();
        public ObservableCollection<MallItemEntry> FilteredAvailableItems
        {
            get { return _filteredAvailableItems; }
            set { SetProperty(ref _filteredAvailableItems, value); }
        }

        private string _availableSearchText = "";
        public string AvailableSearchText
        {
            get { return _availableSearchText; }
            set
            {
                SetProperty(ref _availableSearchText, value);
                FilterAvailableItems();
            }
        }

        // ── Right Panel: Items in mall ──
        private ObservableCollection<MallItemEntry> _mallItems = new ObservableCollection<MallItemEntry>();
        public ObservableCollection<MallItemEntry> MallItems
        {
            get { return _mallItems; }
            set { SetProperty(ref _mallItems, value); }
        }

        private ObservableCollection<MallItemEntry> _filteredMallItems = new ObservableCollection<MallItemEntry>();
        public ObservableCollection<MallItemEntry> FilteredMallItems
        {
            get { return _filteredMallItems; }
            set { SetProperty(ref _filteredMallItems, value); }
        }

        private string _mallSearchText = "";
        public string MallSearchText
        {
            get { return _mallSearchText; }
            set
            {
                SetProperty(ref _mallSearchText, value);
                FilterMallItems();
            }
        }

        // ── Default settings for newly added items ──
        private int _defaultTab;
        public int DefaultTab
        {
            get { return _defaultTab; }
            set { SetProperty(ref _defaultTab, value); }
        }

        private string _defaultPrice = "100";
        public string DefaultPrice
        {
            get { return _defaultPrice; }
            set { SetProperty(ref _defaultPrice, value); }
        }

        private string _defaultAmount = "1";
        public string DefaultAmount
        {
            get { return _defaultAmount; }
            set { SetProperty(ref _defaultAmount, value); }
        }

        private string _defaultDiscount = "100";
        public string DefaultDiscount
        {
            get { return _defaultDiscount; }
            set { SetProperty(ref _defaultDiscount, value); }
        }

        // ── Status ──
        private string _availableCountText = "0 項";
        public string AvailableCountText
        {
            get { return _availableCountText; }
            set { SetProperty(ref _availableCountText, value); }
        }

        private string _mallCountText = "0 項";
        public string MallCountText
        {
            get { return _mallCountText; }
            set { SetProperty(ref _mallCountText, value); }
        }

        public ObservableCollection<string> TabOptions { get; } = new ObservableCollection<string>
        {
            "1-武器", "2-裝備", "3-熱門", "4-雜貨", "5-家具"
        };

        // ── Commands ──
        public DelegateCommand MoveToMallCommand { get; }
        public DelegateCommand MoveFromMallCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ClearAllCommand { get; }
        public DelegateCommand SelectAllAvailableCommand { get; }
        public DelegateCommand SelectAllMallCommand { get; }

        public ItemMallViewModel(IServerEngine engine)
        {
            _engine = engine;

            MoveToMallCommand = new DelegateCommand(OnMoveToMall);
            MoveFromMallCommand = new DelegateCommand(OnMoveFromMall);
            SaveCommand = new DelegateCommand(OnSave);
            ClearAllCommand = new DelegateCommand(OnClearAll);
            SelectAllAvailableCommand = new DelegateCommand(OnSelectAllAvailable);
            SelectAllMallCommand = new DelegateCommand(OnSelectAllMall);

            LoadData();
        }

        void LoadData()
        {
            // Load mall items from engine
            _mallItems.Clear();
            var engineItems = _engine.MallItems;
            if (engineItems != null)
            {
                foreach (var item in engineItems)
                {
                    _mallItems.Add(new MallItemEntry
                    {
                        ItemID = item.ItemID,
                        Name = item.Name,
                        Tab = item.Tab,
                        Price = item.Price,
                        Amount = item.Amount,
                        Discount = item.Discount
                    });
                }
            }

            // Load all items from Item.dat
            LoadAvailableItems();

            FilterAvailableItems();
            FilterMallItems();
            UpdateCounts();
        }

        void LoadAvailableItems()
        {
            _availableItems.Clear();

            if (cGlobal.ItemDatManager == null) return;

            var allItems = cGlobal.ItemDatManager.GetItemList();
            if (allItems == null) return;

            // Build hashset of IDs already in mall
            var mallIds = new HashSet<ushort>(_mallItems.Select(m => m.ItemID));

            foreach (var info in allItems)
            {
                if (info == null || info.ItemID == 0) continue;
                if (mallIds.Contains(info.ItemID)) continue;

                string name = "";
                try { name = Encoding.ASCII.GetString(info.ItemName).TrimEnd('\0'); }
                catch { name = "Item " + info.ItemID; }

                if (string.IsNullOrWhiteSpace(name)) name = "Item " + info.ItemID;

                _availableItems.Add(new MallItemEntry
                {
                    ItemID = info.ItemID,
                    Name = name
                });
            }
        }

        void FilterAvailableItems()
        {
            _filteredAvailableItems.Clear();
            string search = (_availableSearchText ?? "").Trim().ToLowerInvariant();

            foreach (var item in _availableItems)
            {
                if (string.IsNullOrEmpty(search) ||
                    item.Name.ToLowerInvariant().Contains(search) ||
                    item.ItemID.ToString().Contains(search))
                {
                    _filteredAvailableItems.Add(item);
                }
            }
            UpdateCounts();
        }

        void FilterMallItems()
        {
            _filteredMallItems.Clear();
            string search = (_mallSearchText ?? "").Trim().ToLowerInvariant();

            foreach (var item in _mallItems)
            {
                if (string.IsNullOrEmpty(search) ||
                    item.Name.ToLowerInvariant().Contains(search) ||
                    item.ItemID.ToString().Contains(search))
                {
                    _filteredMallItems.Add(item);
                }
            }
            UpdateCounts();
        }

        void UpdateCounts()
        {
            AvailableCountText = _availableItems.Count + " 項" +
                (_filteredAvailableItems.Count != _availableItems.Count
                    ? " (顯示 " + _filteredAvailableItems.Count + ")"
                    : "");

            MallCountText = _mallItems.Count + " / 250 項" +
                (_filteredMallItems.Count != _mallItems.Count
                    ? " (顯示 " + _filteredMallItems.Count + ")"
                    : "");
        }

        void OnMoveToMall()
        {
            var checkedItems = _filteredAvailableItems.Where(i => i.IsChecked).ToList();
            if (checkedItems.Count == 0) return;

            if (_mallItems.Count + checkedItems.Count > 250)
            {
                MessageBox.Show("商城最多 250 個商品，目前已有 " + _mallItems.Count + " 個。",
                    "上限", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ushort price;
            if (!ushort.TryParse(_defaultPrice, out price)) price = 100;
            byte amount;
            if (!byte.TryParse(_defaultAmount, out amount) || amount == 0) amount = 1;
            byte discount;
            if (!byte.TryParse(_defaultDiscount, out discount)) discount = 100;
            byte tab = (byte)(_defaultTab + 1);

            foreach (var item in checkedItems)
            {
                var mallEntry = new MallItemEntry
                {
                    ItemID = item.ItemID,
                    Name = item.Name,
                    Tab = tab,
                    Price = price,
                    Amount = amount,
                    Discount = discount
                };
                _mallItems.Add(mallEntry);
                _availableItems.Remove(item);
            }

            FilterAvailableItems();
            FilterMallItems();
            SyncToEngine();
        }

        void OnMoveFromMall()
        {
            var checkedItems = _filteredMallItems.Where(i => i.IsChecked).ToList();
            if (checkedItems.Count == 0) return;

            foreach (var item in checkedItems)
            {
                _mallItems.Remove(item);
                // Add back to available
                _availableItems.Add(new MallItemEntry
                {
                    ItemID = item.ItemID,
                    Name = item.Name
                });
            }

            FilterAvailableItems();
            FilterMallItems();
            SyncToEngine();
        }

        void SyncToEngine()
        {
            // Sync _mallItems back to engine's BindingList
            var engineItems = _engine.MallItems;
            engineItems.Clear();
            foreach (var entry in _mallItems)
            {
                var item = new ImMallItem(entry.ItemID, entry.Name, entry.Tab, entry.Price);
                item.Amount = entry.Amount;
                item.Discount = entry.Discount;
                engineItems.Add(item);
            }
            _engine.MallManager.InvalidateCache();
            UpdateCounts();
        }

        void OnSave()
        {
            SyncToEngine();
            _engine.MallManager.Save();
            MessageBox.Show("商城設定已儲存（" + _mallItems.Count + " 個商品）。",
                "儲存成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void OnClearAll()
        {
            var result = MessageBox.Show("確定要清除全部商城商品嗎？", "清空全部",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            // Move all mall items back to available
            foreach (var item in _mallItems)
            {
                _availableItems.Add(new MallItemEntry
                {
                    ItemID = item.ItemID,
                    Name = item.Name
                });
            }
            _mallItems.Clear();

            FilterAvailableItems();
            FilterMallItems();
            SyncToEngine();
        }

        void OnSelectAllAvailable()
        {
            bool allChecked = _filteredAvailableItems.All(i => i.IsChecked);
            foreach (var item in _filteredAvailableItems)
                item.IsChecked = !allChecked;
        }

        void OnSelectAllMall()
        {
            bool allChecked = _filteredMallItems.All(i => i.IsChecked);
            foreach (var item in _filteredMallItems)
                item.IsChecked = !allChecked;
        }
    }
}
