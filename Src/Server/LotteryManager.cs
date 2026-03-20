using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DataFiles;

namespace Server
{
    /// <summary>
    /// A single prize entry in the lottery pool.
    /// </summary>
    public class LotteryPrize
    {
        public ushort ItemID { get; set; }
        public string Name { get; set; }
        public byte Amount { get; set; }
        public int Weight { get; set; }

        public LotteryPrize() { Amount = 1; Weight = 100; }
    }

    /// <summary>
    /// Manages the Online Lottery (線上抽獎) prize pools.
    /// Two pools: Gold lottery (type 0) and IM lottery (type 1).
    /// Persists to Data/lottery.txt.
    /// </summary>
    public class LotteryManager
    {
        static readonly string SavePath = Path.Combine("Data", "lottery.txt");

        readonly List<LotteryPrize> _goldPrizes = new List<LotteryPrize>();
        readonly List<LotteryPrize> _imPrizes = new List<LotteryPrize>();
        readonly Random _rng = new Random();
        readonly object _lock = new object();

        /// <summary>Gold lottery cost.</summary>
        public int GoldCost { get; set; }
        /// <summary>IM lottery cost.</summary>
        public int ImCost { get; set; }

        public List<LotteryPrize> GoldPrizes { get { return _goldPrizes; } }
        public List<LotteryPrize> ImPrizes { get { return _imPrizes; } }

        public LotteryManager()
        {
            GoldCost = 1000;
            ImCost = 10;
        }

        /// <summary>
        /// Load lottery config from Data/lottery.txt.
        /// </summary>
        public void Load()
        {
            lock (_lock)
            {
                _goldPrizes.Clear();
                _imPrizes.Clear();
                GoldCost = 1000;
                ImCost = 10;

                if (!File.Exists(SavePath))
                {
                    DebugSystem.Write("[Lottery] No save file found, will auto-populate.");
                    return;
                }

                try
                {
                    string[] lines = File.ReadAllLines(SavePath, Encoding.UTF8);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                        // Config lines: GOLD_COST=1000, IM_COST=10
                        if (line.StartsWith("GOLD_COST="))
                        {
                            int.TryParse(line.Substring(10), out int val);
                            if (val > 0) GoldCost = val;
                            continue;
                        }
                        if (line.StartsWith("IM_COST="))
                        {
                            int.TryParse(line.Substring(8), out int val);
                            if (val > 0) ImCost = val;
                            continue;
                        }

                        // Prize format: Type|ItemID|Name|Amount|Weight
                        // Type: 0=gold, 1=IM, 2=both
                        string[] parts = line.Split('|');
                        if (parts.Length < 5) continue;

                        byte type;
                        ushort itemId;
                        byte amount;
                        int weight;

                        if (!byte.TryParse(parts[0], out type)) continue;
                        if (!ushort.TryParse(parts[1], out itemId)) continue;
                        if (!byte.TryParse(parts[3], out amount)) continue;
                        if (!int.TryParse(parts[4], out weight)) continue;

                        var prize = new LotteryPrize
                        {
                            ItemID = itemId,
                            Name = parts[2],
                            Amount = amount,
                            Weight = weight
                        };

                        if (type == 0 || type == 2) _goldPrizes.Add(prize);
                        if (type == 1 || type == 2)
                        {
                            var copy = new LotteryPrize
                            {
                                ItemID = prize.ItemID,
                                Name = prize.Name,
                                Amount = prize.Amount,
                                Weight = prize.Weight
                            };
                            _imPrizes.Add(copy);
                        }
                    }

                    DebugSystem.Write(string.Format("[Lottery] Loaded {0} gold prizes, {1} IM prizes (cost: {2}g / {3}IM)",
                        _goldPrizes.Count, _imPrizes.Count, GoldCost, ImCost));
                }
                catch (Exception ex)
                {
                    DebugSystem.Write("[Lottery] Load error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Save lottery config to Data/lottery.txt.
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
                    sb.AppendLine("# Online Lottery Config — 線上抽獎設定");
                    sb.AppendLine("# GOLD_COST = gold per draw, IM_COST = IM per draw");
                    sb.AppendLine("GOLD_COST=" + GoldCost);
                    sb.AppendLine("IM_COST=" + ImCost);
                    sb.AppendLine("#");
                    sb.AppendLine("# Prize format: Type|ItemID|Name|Amount|Weight");
                    sb.AppendLine("# Type: 0=gold lottery only, 1=IM lottery only, 2=both");
                    sb.AppendLine("# Weight: higher = more likely to be drawn");

                    // Write gold-only prizes
                    foreach (var p in _goldPrizes)
                    {
                        // Check if also in IM pool
                        bool inIm = _imPrizes.Any(ip => ip.ItemID == p.ItemID && ip.Amount == p.Amount);
                        byte type = inIm ? (byte)2 : (byte)0;
                        sb.AppendLine(string.Format("{0}|{1}|{2}|{3}|{4}", type, p.ItemID, p.Name, p.Amount, p.Weight));
                    }
                    // Write IM-only prizes (not already written as type 2)
                    foreach (var p in _imPrizes)
                    {
                        bool inGold = _goldPrizes.Any(gp => gp.ItemID == p.ItemID && gp.Amount == p.Amount);
                        if (!inGold)
                            sb.AppendLine(string.Format("1|{0}|{1}|{2}|{3}", p.ItemID, p.Name, p.Amount, p.Weight));
                    }

                    File.WriteAllText(SavePath, sb.ToString(), Encoding.UTF8);
                    DebugSystem.Write("[Lottery] Saved to " + SavePath);
                }
                catch (Exception ex)
                {
                    DebugSystem.Write("[Lottery] Save error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Draw a random prize from the specified pool.
        /// Returns null if pool is empty.
        /// </summary>
        public LotteryPrize Draw(byte type)
        {
            lock (_lock)
            {
                var pool = (type == 1) ? _imPrizes : _goldPrizes;
                if (pool.Count == 0) return null;

                int totalWeight = 0;
                foreach (var p in pool) totalWeight += p.Weight;
                if (totalWeight <= 0) return pool[_rng.Next(pool.Count)];

                int roll = _rng.Next(totalWeight);
                int cumulative = 0;
                foreach (var p in pool)
                {
                    cumulative += p.Weight;
                    if (roll < cumulative) return p;
                }

                return pool[pool.Count - 1];
            }
        }

        /// <summary>
        /// Auto-populate lottery prizes from the Item Mall item list.
        /// </summary>
        public void AutoPopulate(PhxItemDat itemDat)
        {
            if (itemDat == null) return;
            if (_goldPrizes.Count > 0 || _imPrizes.Count > 0) return;

            // Use consumables/food items for gold lottery, and better items for IM lottery
            var allItems = itemDat.GetItemList();
            if (allItems == null || allItems.Count == 0) return;

            foreach (var info in allItems)
            {
                if (info == null || info.ItemID == 0) continue;

                string name = "";
                try { name = Encoding.ASCII.GetString(info.ItemName).TrimEnd('\0'); }
                catch { name = "Item " + info.ItemID; }

                byte itemType = info.ItemType;

                // Food/consumables (type 20,23,31,32) → gold lottery
                if (itemType == 20 || itemType == 23 || itemType == 31 || itemType == 32)
                {
                    _goldPrizes.Add(new LotteryPrize { ItemID = info.ItemID, Name = name, Amount = 1, Weight = 100 });
                    if (_goldPrizes.Count >= 20) continue;
                }

                // Weapons/armor (type 3-9, 1-2, 10-16) → IM lottery
                if ((itemType >= 1 && itemType <= 9) || (itemType >= 10 && itemType <= 16))
                {
                    _imPrizes.Add(new LotteryPrize { ItemID = info.ItemID, Name = name, Amount = 1, Weight = 50 });
                    if (_imPrizes.Count >= 20) continue;
                }

                if (_goldPrizes.Count >= 20 && _imPrizes.Count >= 20) break;
            }

            // If we didn't get enough, add some from immall
            if (_goldPrizes.Count == 0 && _imPrizes.Count == 0)
            {
                // Fallback: use some hardcoded common items
                AddFallbackPrizes();
            }

            DebugSystem.Write(string.Format("[Lottery] Auto-populated {0} gold prizes, {1} IM prizes",
                _goldPrizes.Count, _imPrizes.Count));
        }

        void AddFallbackPrizes()
        {
            // Common food/consumable items from immall.txt
            _goldPrizes.Add(new LotteryPrize { ItemID = 32001, Name = "White Rice", Amount = 5, Weight = 200 });
            _goldPrizes.Add(new LotteryPrize { ItemID = 32011, Name = "Bread", Amount = 5, Weight = 200 });
            _goldPrizes.Add(new LotteryPrize { ItemID = 32005, Name = "Seafood Rice Noodles", Amount = 3, Weight = 150 });
            _goldPrizes.Add(new LotteryPrize { ItemID = 32028, Name = "Sashimi", Amount = 3, Weight = 100 });
            _goldPrizes.Add(new LotteryPrize { ItemID = 33017, Name = "Strengthening Potion", Amount = 1, Weight = 50 });
            _goldPrizes.Add(new LotteryPrize { ItemID = 33022, Name = "Protection Potion", Amount = 1, Weight = 50 });
            _goldPrizes.Add(new LotteryPrize { ItemID = 33024, Name = "Swift Potion", Amount = 1, Weight = 50 });
            _goldPrizes.Add(new LotteryPrize { ItemID = 25043, Name = "Phoenix Feather", Amount = 1, Weight = 20 });

            // Weapons for IM lottery
            _imPrizes.Add(new LotteryPrize { ItemID = 10046, Name = "Wind Rip", Amount = 1, Weight = 100 });
            _imPrizes.Add(new LotteryPrize { ItemID = 10047, Name = "Crystal Knife", Amount = 1, Weight = 80 });
            _imPrizes.Add(new LotteryPrize { ItemID = 10050, Name = "Arabia Knife", Amount = 1, Weight = 60 });
            _imPrizes.Add(new LotteryPrize { ItemID = 12063, Name = "Knight's Spear", Amount = 1, Weight = 50 });
            _imPrizes.Add(new LotteryPrize { ItemID = 12065, Name = "Dragon's Wing Spear", Amount = 1, Weight = 30 });
            _imPrizes.Add(new LotteryPrize { ItemID = 25043, Name = "Phoenix Feather", Amount = 1, Weight = 40 });
            _imPrizes.Add(new LotteryPrize { ItemID = 33017, Name = "Strengthening Potion", Amount = 3, Weight = 100 });
            _imPrizes.Add(new LotteryPrize { ItemID = 30340, Name = "Mysterious Pumpkin", Amount = 1, Weight = 20 });
        }
    }
}
