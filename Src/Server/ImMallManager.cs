using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Game;
using Network;
using DataFiles;

namespace Server
{
    /// <summary>
    /// A single item listed in the Item Mall (Cash Shop).
    /// </summary>
    public class ImMallItem
    {
        public ushort ItemID { get; set; }
        public string Name { get; set; }
        public byte Amount { get; set; }
        public ushort Price { get; set; }
        public byte Discount { get; set; }
        public byte State { get; set; }
        public byte Tab { get; set; }

        public ImMallItem() { Amount = 1; Discount = 100; }

        public ImMallItem(ushort itemID, string name, byte tab, ushort price)
        {
            ItemID = itemID;
            Name = name;
            Tab = tab;
            Price = price;
            Amount = 1;
            Discount = 100;
            State = 0;
        }
    }

    /// <summary>
    /// Manages the Item Mall item list.
    /// Persists to Data/immall.json. Builds AC 75,1 packets.
    /// </summary>
    public class ImMallManager
    {
        static readonly string SavePath = Path.Combine("Data", "immall.txt");

        readonly BindingList<ImMallItem> _items = new BindingList<ImMallItem>();
        SendPacket _cachedPacket;
        readonly object _lock = new object();

        public BindingList<ImMallItem> Items { get { return _items; } }

        public ImMallManager()
        {
            _items.ListChanged += (s, e) => InvalidateCache();
        }

        /// <summary>
        /// Load mall items from disk.
        /// </summary>
        public void Load()
        {
            lock (_lock)
            {
                _items.Clear();
                if (!File.Exists(SavePath))
                {
                    DebugSystem.Write("[ImMall] No save file found, starting empty.");
                    return;
                }

                try
                {
                    string[] lines = File.ReadAllLines(SavePath, Encoding.UTF8);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                        // Format: ItemID|Name|Tab|Price|Amount|Discount|State
                        string[] parts = line.Split('|');
                        if (parts.Length < 7) continue;

                        var item = new ImMallItem();
                        item.ItemID = ushort.Parse(parts[0]);
                        item.Name = parts[1];
                        item.Tab = byte.Parse(parts[2]);
                        item.Price = ushort.Parse(parts[3]);
                        item.Amount = byte.Parse(parts[4]);
                        item.Discount = byte.Parse(parts[5]);
                        item.State = byte.Parse(parts[6]);
                        _items.Add(item);
                    }
                    DebugSystem.Write("[ImMall] Loaded " + _items.Count + " items from " + SavePath);
                }
                catch (Exception ex)
                {
                    DebugSystem.Write("[ImMall] Load error: " + ex.Message);
                }
                InvalidateCache();
            }
        }

        /// <summary>
        /// Save mall items to disk.
        /// </summary>
        public void Save()
        {
            lock (_lock)
            {
                try
                {
                    string dir = Path.GetDirectoryName(SavePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    var sb = new StringBuilder();
                    sb.AppendLine("# Item Mall Data — ItemID|Name|Tab|Price|Amount|Discount|State");
                    foreach (var item in _items)
                    {
                        sb.AppendLine(string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}",
                            item.ItemID, item.Name, item.Tab, item.Price, item.Amount, item.Discount, item.State));
                    }
                    File.WriteAllText(SavePath, sb.ToString(), Encoding.UTF8);
                    DebugSystem.Write("[ImMall] Saved " + _items.Count + " items to " + SavePath);
                }
                catch (Exception ex)
                {
                    DebugSystem.Write("[ImMall] Save error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Add an item to the mall.
        /// </summary>
        public void AddItem(ImMallItem item)
        {
            lock (_lock)
            {
                _items.Add(item);
            }
        }

        /// <summary>
        /// Remove an item by index.
        /// </summary>
        public void RemoveAt(int index)
        {
            lock (_lock)
            {
                if (index >= 0 && index < _items.Count)
                    _items.RemoveAt(index);
            }
        }

        /// <summary>
        /// Invalidate the cached packet so it's rebuilt on next request.
        /// </summary>
        public void InvalidateCache()
        {
            lock (_lock)
            {
                _cachedPacket = null;
            }
        }

        /// <summary>
        /// Build AC 75,1 packet with all mall items.
        /// Format: [75][1][count:u16] then per item:
        /// [itemID:u16][ammt:u8][price:u16][discount:u8][state:u8][tab:u8][entryId:u8][0:u8]
        /// </summary>
        public SendPacket BuildPacket()
        {
            lock (_lock)
            {
                if (_cachedPacket != null)
                    return _cachedPacket;

                SendPacket pkt = new SendPacket();
                pkt.PackArray(new byte[] { 75, 1 });

                int count = Math.Min(_items.Count, 250);
                pkt.Pack16((ushort)count);

                for (int i = 0; i < count; i++)
                {
                    var item = _items[i];
                    byte entryId = (byte)(i + 1);

                    pkt.Pack16(item.ItemID);
                    pkt.Pack8(item.Amount);
                    pkt.Pack16(item.Price);
                    pkt.Pack8(item.Discount);
                    pkt.Pack8(item.State);
                    pkt.Pack8(item.Tab);
                    pkt.Pack8(entryId);
                    pkt.Pack8(0);
                }

                DebugSystem.Write(string.Format("[ImMall] Packet built: {0} items", count));
                _cachedPacket = pkt;
                return _cachedPacket;
            }
        }

        /// <summary>
        /// Auto-populate from PhxItemDat if the mall is empty.
        /// Picks items by type and assigns tabs.
        /// </summary>
        public void AutoPopulate(PhxItemDat itemDat)
        {
            if (itemDat == null) return;
            if (_items.Count > 0) return; // already has items

            var allItems = itemDat.GetItemList();
            if (allItems == null || allItems.Count == 0) return;

            const int MAX_PER_TAB = 30;
            int[] tabCounts = new int[6];

            foreach (var info in allItems)
            {
                if (info == null || info.ItemID == 0) continue;

                byte tab = GetTabForItemType(info.ItemType);
                if (tab == 0) continue;
                if (tabCounts[tab] >= MAX_PER_TAB) continue;

                tabCounts[tab]++;
                string name = "";
                try { name = Encoding.ASCII.GetString(info.ItemName).TrimEnd('\0'); }
                catch { name = "Item " + info.ItemID; }

                ushort price = (ushort)Math.Max(1, info.rank * 5 + 10);
                _items.Add(new ImMallItem(info.ItemID, name, tab, price));

                if (_items.Count >= 250) break;
            }

            DebugSystem.Write(string.Format("[ImMall] Auto-populated {0} items (W={1} A={2} H={3} G={4} F={5})",
                _items.Count, tabCounts[1], tabCounts[2], tabCounts[3], tabCounts[4], tabCounts[5]));
            InvalidateCache();
        }

        static byte GetTabForItemType(byte itemType)
        {
            switch (itemType)
            {
                case 3: case 4: case 5: case 6: case 8: case 9:
                    return 1; // Weaponry
                case 1: case 2: case 10: case 11: case 12: case 13: case 14: case 15: case 16:
                    return 2; // Armory
                case 24: case 25: case 40:
                    return 3; // HOT
                case 20: case 23: case 31: case 32:
                    return 4; // Grocery
                case 27: case 30:
                    return 5; // Furniture
                default:
                    return 0;
            }
        }
    }
}
