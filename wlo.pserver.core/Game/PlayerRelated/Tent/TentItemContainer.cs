using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network;

namespace Game.Code
{
    /// <summary>
    /// Manages placed items on a single tent floor.
    /// Each floor can hold up to MAX_ITEMS placed objects.
    /// </summary>
    public class TentFloor
    {
        const int MAX_ITEMS = 30;

        readonly object m_lock = new object();
        Dictionary<byte, PlacedItem> m_items;

        public ushort FloorColor { get; set; }
        public ushort Wallpaper { get; set; }

        public TentFloor()
        {
            m_items = new Dictionary<byte, PlacedItem>(MAX_ITEMS);
            FloorColor = 39062;
            Wallpaper = 39064;
        }

        public int Count { get { lock (m_lock) return m_items.Count; } }

        /// <summary>
        /// Place a new item on this floor.
        /// Returns the assigned slot index, or 0 if floor is full.
        /// </summary>
        public byte PlaceItem(ushort itemID, ushort x, ushort y, ushort z, byte rotation)
        {
            lock (m_lock)
            {
                if (m_items.Count >= MAX_ITEMS) return 0;

                byte slot = 1;
                while (m_items.ContainsKey(slot) && slot < MAX_ITEMS + 1) slot++;
                if (slot > MAX_ITEMS) return 0;

                m_items[slot] = new PlacedItem
                {
                    ItemID = itemID,
                    X = x,
                    Y = y,
                    Z = z,
                    Rotation = rotation
                };
                return slot;
            }
        }

        /// <summary>
        /// Remove a placed item. Returns the item ID, or 0 if not found.
        /// </summary>
        public ushort RemoveItem(byte slot)
        {
            lock (m_lock)
            {
                if (!m_items.ContainsKey(slot)) return 0;
                ushort id = m_items[slot].ItemID;
                m_items.Remove(slot);
                return id;
            }
        }

        /// <summary>
        /// Move/rotate a placed item.
        /// </summary>
        public bool MoveItem(byte slot, ushort x, ushort y, byte rotation)
        {
            lock (m_lock)
            {
                if (!m_items.ContainsKey(slot)) return false;
                m_items[slot].X = x;
                m_items[slot].Y = y;
                m_items[slot].Rotation = rotation;
                return true;
            }
        }

        /// <summary>
        /// Build AC 62,4 packet with all placed items on this floor.
        /// </summary>
        public SendPacket GetItemListPacket(byte floorIndex)
        {
            lock (m_lock)
            {
                if (m_items.Count == 0) return null;

                SendPacket pkt = new SendPacket();
                pkt.Pack8(62);
                pkt.Pack8(4);
                pkt.Pack8(floorIndex);
                pkt.Pack8((byte)m_items.Count);

                foreach (var kvp in m_items)
                {
                    pkt.Pack8(kvp.Key);
                    pkt.Pack16(kvp.Value.ItemID);
                    pkt.Pack16(kvp.Value.X);
                    pkt.Pack16(kvp.Value.Y);
                    pkt.Pack16(kvp.Value.Z);
                    pkt.Pack8(kvp.Value.Rotation);
                }

                return pkt;
            }
        }

        /// <summary>
        /// Serialize floor items to a DB-friendly string.
        /// Format: slot|itemID|x|y|z|rotation, separated by &amp;
        /// </summary>
        public string SaveToDB()
        {
            lock (m_lock)
            {
                if (m_items.Count == 0) return "none";
                var parts = new List<string>();
                foreach (var kvp in m_items)
                {
                    var item = kvp.Value;
                    parts.Add(string.Format("{0}|{1}|{2}|{3}|{4}|{5}",
                        kvp.Key, item.ItemID, item.X, item.Y, item.Z, item.Rotation));
                }
                return string.Join("&", parts);
            }
        }

        /// <summary>
        /// Load floor items from DB string.
        /// </summary>
        public void LoadFromDB(string data)
        {
            lock (m_lock)
            {
                m_items.Clear();
                if (string.IsNullOrEmpty(data) || data == "none") return;

                foreach (string entry in data.Split('&'))
                {
                    string[] parts = entry.Split('|');
                    if (parts.Length < 6) continue;

                    byte slot = byte.Parse(parts[0]);
                    m_items[slot] = new PlacedItem
                    {
                        ItemID = ushort.Parse(parts[1]),
                        X = ushort.Parse(parts[2]),
                        Y = ushort.Parse(parts[3]),
                        Z = ushort.Parse(parts[4]),
                        Rotation = byte.Parse(parts[5])
                    };
                }
            }
        }
    }

    /// <summary>
    /// A single placed item in a tent floor.
    /// </summary>
    public class PlacedItem
    {
        public ushort ItemID { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public ushort Z { get; set; }
        public byte Rotation { get; set; }
    }
}
