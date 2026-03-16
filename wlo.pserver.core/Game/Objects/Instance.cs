using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network;

namespace Game
{
    /// <summary>
    /// Manages all active instances (dungeon parties) in the game.
    /// An instance is a group of players who enter a dungeon map together.
    /// </summary>
    public class InstanceSystem
    {
        readonly object m_lock = new object();
        Dictionary<int, DungeonInstance> m_instances;
        Dictionary<int, InstanceData> m_data;

        int Tabs
        {
            get
            {
                int count = m_instances.Count;
                if (count <= 5) return 1;
                if (count <= 10) return 2;
                if (count <= 15) return 3;
                if (count <= 20) return 4;
                return 1;
            }
        }

        public InstanceSystem()
        {
            m_instances = new Dictionary<int, DungeonInstance>();
            m_data = new Dictionary<int, InstanceData>();
            LoadData();
        }

        void LoadData()
        {
            // Hardcoded dungeon definitions — will be loaded from DB/files later
            m_data[30012] = new InstanceData
            {
                IDGlobal = 61591,
                Name = "Yoyo Family",
                MaxPlayers = 6,
                LevelRestrict = 10,
                Timer = 15
            };

            m_data[30013] = new InstanceData
            {
                IDGlobal = 61592,
                Name = "Slime Cave",
                MaxPlayers = 4,
                LevelRestrict = 20,
                Timer = 20
            };

            m_data[30014] = new InstanceData
            {
                IDGlobal = 61593,
                Name = "Dark Forest",
                MaxPlayers = 6,
                LevelRestrict = 30,
                Timer = 30
            };
        }

        int GetTabForIndex(int count)
        {
            if (count <= 5) return 1;
            if (count <= 10) return 2;
            if (count <= 15) return 3;
            if (count <= 20) return 4;
            return 5;
        }

        int GetTabSkip(int tab)
        {
            return (tab - 1) * 5;
        }

        int GetNumberPerTab(int tab, int total)
        {
            int start = (tab - 1) * 5;
            int remaining = total - start;
            return Math.Min(remaining, 5);
        }

        int AllocateInstanceID(int dataID)
        {
            if (!m_data.ContainsKey(dataID)) return -1;
            int id = m_data[dataID].IDGlobal;
            while (m_instances.ContainsKey(id)) id++;
            return id;
        }

        /// <summary>
        /// Create a new instance party.
        /// </summary>
        public void CreateInstance(Player src, int dataID, string text)
        {
            lock (m_lock)
            {
                int id = AllocateInstanceID(dataID);
                if (id < 0) return;

                var inst = new DungeonInstance();
                inst.ID = id;
                inst.Text = text;
                inst.CreatorID = src.CharID;
                inst.CreatorName = src.CharName;
                inst.DataID = dataID;
                inst.Members[src.CharID] = src;

                m_instances[id] = inst;
                src.CurInstance = id;

                // AC 85,8 — broadcast new instance to all members' maps
                SendPacket s = new SendPacket();
                s.PackArray(new byte[] { 85, 8 });
                s.Pack8((byte)GetTabForIndex(m_instances.Count));
                s.Pack16((ushort)id);
                s.PackStringN(src.CharName);
                s.PackStringN(text);
                s.Pack32(1);
                s.Pack32(0);
                // Send to creator only (no global broadcast in new system)
                src.Send(s);

                // AC 85,5 — instance join confirmation to creator
                s = new SendPacket();
                s.PackArray(new byte[] { 85, 5, 1, 1 });
                s.Pack16((ushort)id);
                s.Pack8(1);
                s.PackStringN(text);
                s.Pack32(src.CharID);
                src.Send(s);

                // AC 85,2 — close instance list UI
                s = new SendPacket();
                s.PackArray(new byte[] { 85, 2, 0 });
                src.Send(s);
            }
        }

        /// <summary>
        /// Send list of active instances to player (paginated by tab).
        /// </summary>
        public void SendInstanceList(Player src, int tab)
        {
            lock (m_lock)
            {
                SendPacket s = new SendPacket();
                s.PackArray(new byte[] { 85, 1 });
                s.Pack8((byte)Tabs);
                s.Pack8((byte)tab);

                if (m_instances.Count > 0)
                {
                    var page = m_instances.Skip(GetTabSkip(tab)).Take(5).ToList();
                    s.Pack8((byte)page.Count);

                    foreach (var kvp in page)
                    {
                        s.Pack16((ushort)kvp.Value.ID);
                        s.PackStringN(kvp.Value.CreatorName);
                        s.PackStringN(kvp.Value.Text);
                        s.Pack32(1);
                        s.Pack32(0);
                    }
                }
                else
                {
                    s.Pack8(0);
                }
                src.Send(s);

                s = new SendPacket();
                s.PackArray(new byte[] { 85, 13, 0, 0 });
                src.Send(s);
            }
        }

        /// <summary>
        /// Preview instance members before joining.
        /// </summary>
        public void PreJoin(Player src, int instanceID)
        {
            lock (m_lock)
            {
                if (!m_instances.ContainsKey(instanceID)) return;
                var inst = m_instances[instanceID];

                SendPacket s = new SendPacket();
                s.PackArray(new byte[] { 85, 4 });
                s.Pack16((ushort)inst.ID);
                s.Pack8((byte)inst.Members.Count);

                foreach (var pair in inst.Members)
                {
                    s.PackStringN(pair.Value.CharName);
                    s.Pack32(pair.Value.CharID);
                }
                src.Send(s);
            }
        }

        /// <summary>
        /// Join an existing instance.
        /// </summary>
        public void JoinInstance(Player src, int instanceID)
        {
            lock (m_lock)
            {
                if (!m_instances.ContainsKey(instanceID)) return;
                var inst = m_instances[instanceID];

                // Check capacity
                if (m_data.ContainsKey(inst.DataID) && inst.Members.Count >= m_data[inst.DataID].MaxPlayers)
                    return;

                // Check level requirement
                if (m_data.ContainsKey(inst.DataID) && src.Level < m_data[inst.DataID].LevelRestrict)
                    return;

                // AC 85,7 — notify existing members
                SendPacket s = new SendPacket();
                s.PackArray(new byte[] { 85, 7 });
                s.Pack32(1);
                s.Pack8((byte)(inst.Members.Count + 1));
                s.Pack32(src.CharID);
                s.Pack16((ushort)inst.ID);
                BroadcastToInstance(inst, s);

                inst.Members[src.CharID] = src;
                src.CurInstance = inst.ID;

                // AC 85,5 — join confirmation to new member
                s = new SendPacket();
                s.PackArray(new byte[] { 85, 5 });
                s.Pack8(1);
                s.Pack8(1);
                s.Pack16((ushort)inst.ID);
                s.Pack8((byte)inst.Members.Count);
                s.PackStringN(inst.Text);
                s.Pack32(inst.CreatorID);
                src.Send(s);

                // AC 85,3 — close UI
                s = new SendPacket();
                s.PackArray(new byte[] { 85, 3, 0 });
                src.Send(s);
            }
        }

        /// <summary>
        /// Exit current instance.
        /// </summary>
        public void ExitInstance(Player src)
        {
            lock (m_lock)
            {
                int instID = src.CurInstance;
                if (!m_instances.ContainsKey(instID)) return;

                var inst = m_instances[instID];
                int remaining = inst.Members.Count - 1;
                if (remaining < 0) remaining = 0;

                // AC 85,12 — notify all that someone left
                SendPacket s = new SendPacket();
                s.PackArray(new byte[] { 85, 12 });
                s.Pack8((byte)remaining);
                s.Pack32(src.CharID);
                s.Pack16((ushort)instID);
                s.Pack8((inst.CreatorID == src.CharID) ? (byte)1 : (byte)0);
                BroadcastToInstance(inst, s);

                // Remove player
                inst.RemoveMember(src.CharID);

                if (remaining == 0)
                {
                    // Remove empty instance
                    m_instances.Remove(instID);

                    s = new SendPacket();
                    s.PackArray(new byte[] { 85, 11, 2 });
                    s.Pack16((ushort)instID);
                    // No global broadcast — instance is gone
                }

                // AC 85,5,2 — confirm exit to player
                s = new SendPacket();
                s.PackArray(new byte[] { 85, 5, 2 });
                src.Send(s);
            }
        }

        /// <summary>
        /// Check member list of current instance (paginated).
        /// </summary>
        public void CheckMembers(Player src, byte tab)
        {
            lock (m_lock)
            {
                if (src.CurInstance == 0) return;
                if (!m_instances.ContainsKey(src.CurInstance)) return;

                var inst = m_instances[src.CurInstance];
                int total = inst.Members.Count;
                int perTab = GetNumberPerTab(tab, total);

                SendPacket s = new SendPacket();
                s.PackArray(new byte[] { 85, 6 });
                s.Pack8((byte)inst.Tabs);
                s.Pack8(tab);
                s.Pack8((byte)total);
                s.Pack8((byte)perTab);

                var page = inst.Members.Skip(GetTabSkip(tab)).Take(5).ToList();
                foreach (var pair in page)
                {
                    s.Pack32(pair.Value.CharID);
                }
                src.Send(s);
            }
        }

        /// <summary>
        /// Dismiss a member from the instance (creator only).
        /// </summary>
        public void DismissMember(Player src, uint memberID)
        {
            lock (m_lock)
            {
                if (src.CurInstance == 0) return;
                if (!m_instances.ContainsKey(src.CurInstance)) return;

                var inst = m_instances[src.CurInstance];
                if (inst.CreatorID != src.CharID) return; // only creator can dismiss

                if (!inst.Members.ContainsKey(memberID)) return;

                Player target = inst.Members[memberID];
                inst.RemoveMember(memberID);

                // AC 85,12 — notify remaining members
                SendPacket s = new SendPacket();
                s.PackArray(new byte[] { 85, 12 });
                s.Pack8((byte)inst.Members.Count);
                s.Pack32(memberID);
                s.Pack16((ushort)src.CurInstance);
                s.Pack8(0); // 0 = member, 1 = creator
                BroadcastToInstance(inst, s);

                // AC 85,5,4 — notify dismissed player
                s = new SendPacket();
                s.PackArray(new byte[] { 85, 5, 4 });
                target.Send(s);
            }
        }

        /// <summary>
        /// Get instance data by data ID (for scene integration).
        /// </summary>
        public InstanceData GetData(int dataID)
        {
            if (m_data.ContainsKey(dataID))
                return m_data[dataID];
            return null;
        }

        void BroadcastToInstance(DungeonInstance inst, SendPacket pkt)
        {
            foreach (var pair in inst.Members)
            {
                pair.Value.Send(pkt);
            }
        }
    }

    /// <summary>
    /// A single active dungeon instance (party).
    /// </summary>
    class DungeonInstance
    {
        public int ID;
        public int DataID;
        public string Text;
        public uint CreatorID;
        public string CreatorName;
        public int TimerElapsed;
        public Dictionary<uint, Player> Members = new Dictionary<uint, Player>();

        public int Tabs
        {
            get
            {
                int count = Members.Count;
                if (count <= 5) return 1;
                if (count <= 10) return 2;
                if (count <= 15) return 3;
                if (count <= 20) return 4;
                return 1;
            }
        }

        public void RemoveMember(uint charID)
        {
            if (Members.ContainsKey(charID))
            {
                Members[charID].CurInstance = 0;
                Members.Remove(charID);
            }
        }
    }

    /// <summary>
    /// Static data for a dungeon type (loaded from config/DB).
    /// </summary>
    public class InstanceData
    {
        public int IDGlobal;
        public string Name;
        public int MaxPlayers;
        public int Timer;
        public byte LevelRestrict;
    }
}
