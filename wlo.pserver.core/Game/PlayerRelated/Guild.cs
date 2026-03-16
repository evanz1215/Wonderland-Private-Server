using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Code;
using Game.Maps;
using Network;

namespace Game
{
    /// <summary>
    /// Manages all Guilds in game
    /// </summary>
    public class GuildSystem
    {
        readonly object mylock = new object();
        Dictionary<UInt16, Guild> GlobalGuild;

        ushort AvailableGuildID { get { ushort a = 1000; while (GlobalGuild.ContainsKey(a)) { a++; } return a; } }

        public GuildSystem()
        {
            GlobalGuild = new Dictionary<UInt16, Guild>();
        }

        public bool CreateNewGuild(Player src, string GuildName)
        {
            lock (mylock)
            {
                if (GlobalGuild.Values.Count(c => c.GuildName == GuildName) > 0)
                    return false;

                if (src.Level < 1) // should be 20
                    return false;

                if (src.Gold < 1) // should be 100000
                    return false;

                try
                {
                    src.TakeGold(20000);

                    Guild cg = new Guild();
                    cg.GuildName = GuildName;
                    cg.GuildID = AvailableGuildID;
                    cg.Leader = new GuildMember(src);
                    cg.DateCreator = DateTime.Today;
                    cg.IconGuil = 3402; // default insignia
                    cg.Rules = "";

                    SendPacket s = new SendPacket();
                    s.PackArray(new byte[] { 39, 1, 0 }); // clean Tab guild
                    src.Send(s);

                    cg.AddMember(src, true);

                    GlobalGuild.Add(cg.GuildID, cg);

                    onPlayerLogin(src, cg.GuildID);
                }
                catch { return false; }

                return true;
            }
        }

        public void onPlayerLogin(Player src, ushort guildID)
        {
            if (GlobalGuild.ContainsKey(guildID))
            {
                src.CurGuild = GlobalGuild[guildID];
                GlobalGuild[guildID].SendInfo(src);
            }
        }

        public Guild GetGuild(ushort guildID)
        {
            if (GlobalGuild.ContainsKey(guildID))
                return GlobalGuild[guildID];
            return null;
        }

        public void RemoveGuild(ushort guildID)
        {
            lock (mylock)
            {
                if (GlobalGuild.ContainsKey(guildID))
                    GlobalGuild.Remove(guildID);
            }
        }
    }


    public class Guild
    {
        #region Properties

        public UInt16 GuildID;
        public string GuildName;
        public string Rules;
        public uint IconGuil;
        public DateTime DateCreator;

        public GuildMember Leader;
        List<GuildMember> ViceOrg = new List<GuildMember>(4);
        List<GuildMember> Members = new List<GuildMember>(50);
        List<byte> ImgInsigne = new List<byte>();

        // Guild warehouse
        List<Item> Warehouse = new List<Item>();
        const int WAREHOUSE_MAX = 50;

        public int TotalMembers { get { return (4 + (Members.Count - TotalViceOrg)); } }
        public int TotalViceOrg { get { return (ViceOrg.Count(c => c.ID > 0)); } }

        public Dictionary<uint, GuildMember> MembersOnline
        {
            get
            {
                Dictionary<uint, GuildMember> tmp = new Dictionary<uint, GuildMember>();
                foreach (var t in Members.Where(c => c.isOnline).ToList())
                    tmp.Add(t.ID, t);
                return tmp;
            }
        }

        public Guild()
        {
        }
        #endregion

        public void ChangePermissionMember(uint target, RecievePacket r)
        {
            if (Members.Count(c => c.ID == target) > 0)
            {
                r.SetPtr(8);
                Members.Find(c => c.ID == target).can_Invite = Convert.ToBoolean(r.Unpack8());
                r.SetPtr(10);
                Members.Find(c => c.ID == target).can_Modify_Rights = Convert.ToBoolean(r.Unpack8());

                int b = 8;
                for (int a = 1; a < 6; a++)
                {
                    r.SetPtr(b);
                    byte val = r.Unpack8();

                    SendPacket s = new SendPacket();
                    s.PackArray(new byte[] { 39, 24 });
                    s.Pack32(target);
                    s.Pack32(1);
                    s.Pack8((byte)a);
                    s.Pack8(val);
                    BroadCastGuild(s, 0);

                    // Send to the target directly if online
                    var mem = Members.Find(c => c.ID == target);
                    if (mem != null && mem.isOnline)
                        ((Player)mem.OnlineSrc).Send(s);

                    b += 2;
                }
            }
        }

        public void HoldThePostOfViceOrgleader(uint target, byte type)
        {
            if (Members.Count(c => c.ID == target) > 0)
            {
                var tmp = Members.Find(c => c.ID == target);
                ViceOrg.Add(tmp);

                SendPacket s = new SendPacket();
                s.PackArray(new byte[] { 39, 12 });
                s.Pack32(target);
                BroadCastGuild(s, 0);
            }
        }

        public void RemoveHoldThePostOfViceOrgleader(Player src, uint target)
        {
            if (Members.Count(c => c.ID == target) > 0)
            {
                var tmp = Members.Find(c => c.ID == target);
                if (tmp != null)
                {
                    ViceOrg.Remove(tmp);

                    SendPacket s = new SendPacket();
                    s.PackArray(new byte[] { 39, 13 });
                    s.Pack32(target);
                    BroadCastGuild(s, 0);

                    s = new SendPacket();
                    s.PackArray(new byte[] { 39, 15, 0 });
                    s.Pack32(target);
                    src.Send(s);
                }
            }
        }

        public void AddNewMemberGuild(Player src, uint Actor)
        {
            if (Members.Count(c => c.ID == src.CharID) > 0) return;

            SendPacket s = new SendPacket();
            s.PackArray(new byte[] { 39, 4 });
            s.Pack32(src.CharID);
            s.Pack32(1);
            src.Send(s);

            // Notify the actor
            var actorMem = Members.Find(c => c.ID == Actor);
            if (actorMem != null && actorMem.isOnline)
                ((Player)actorMem.OnlineSrc).Send(s);

            AddMember(src, false);
            SendInfo(src);

            s = new SendPacket();
            s.PackArray(new byte[] { 39, 62, 1 });
            src.Send(s);

            GetGuilNickName(src);
            GetInsigneGuild(src);

            Send39_8(src);
            Send39_60(src);
            Send39_61(src);

            s = new SendPacket();
            s.PackArray(new byte[] { 24, 5, 1, 1, 0 });
            src.Send(s);
        }

        public void AddMember(Player src, bool Master)
        {
            if (Members.Count(c => c.ID == src.CharID) > 0) return;

            GuildMember mw = new GuildMember();
            mw.OnlineSrc = src;

            if (Master)
            {
                mw.can_Modify_Badge = true;
                mw.can_Invite = true;
                mw.can_DisBand_Guild = true;
                mw.can_Modify_Rights = true;
                mw.can_Modify_Rules = true;
            }

            Members.Add(mw);
        }

        public bool SendInfo(Player src)
        {
            Send39_2(src);
            Send39_21_26_27(src);
            GetMemberOnline(src);
            GetMemberState(src);
            GetGuilNickName(src);
            return true;
        }

        #region Packets Login

        void Send39_2(Player src)
        {
            SendPacket s = new SendPacket();
            s.PackArray(new byte[] { 39, 2 });
            s.PackStringN(GuildName);
            s.Pack8((byte)TotalViceOrg);
            s.Pack8((byte)TotalMembers);

            foreach (var pair in Members.ToList())
            {
                s.Pack32(pair.ID);
                s.PackStringN(pair.CharacterName);
                s.Pack8(pair.Level);
                s.Pack8((byte)pair.Job);
                s.Pack8(pair.Reborn ? (byte)1 : (byte)0);
                s.Pack8((byte)pair.Element);
                s.Pack8((byte)pair.Body);
                s.Pack8(pair.Head);
                s.Pack16(pair.HairColor);
                s.Pack16(pair.SkinColor);
                s.Pack16(pair.ClothingColor);
                s.Pack16(pair.EyeColor);
                s.PackStringN(pair.Nickname);
                s.Pack32(0);
                s.Pack8(pair.can_DisBand_Guild ? (byte)1 : (byte)0);
                s.Pack8(pair.can_Invite ? (byte)1 : (byte)0);
                s.Pack8(pair.can_Modify_Rules ? (byte)1 : (byte)0);
                s.Pack8(pair.can_Modify_Rights ? (byte)1 : (byte)0);
                s.Pack8(pair.can_Modify_Badge ? (byte)1 : (byte)0);
                s.Pack32(0);
                s.Pack32(0);
            }

            s.PackStringN(Rules);
            s.PackArray(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 157,
                115, 241, 148, 144, 109, 228, 64, 0 });
            src.Send(s);
        }

        void Send39_17(Player src)
        {
            if (Members.Count(c => c.ID == src.CharID) > 0)
            {
                SendPacket s = new SendPacket();
                s.PackArray(new byte[] { 39, 17 });
                src.Send(s);
            }
        }

        void Send39_21_26_27(Player src)
        {
            if (Members.Count(c => c.ID == src.CharID) > 0)
            {
                for (int a = 1; a < 8; a++)
                {
                    SendPacket s = new SendPacket();
                    s.PackArray(new byte[] { 39, 21 });
                    s.Pack8((byte)a);
                    s.Pack32(0);
                    src.Send(s);
                }
                for (int a = 1; a < 5; a++)
                {
                    SendPacket s = new SendPacket();
                    s.PackArray(new byte[] { 39, 26 });
                    s.Pack8((byte)a);
                    s.Pack32(0);
                    src.Send(s);
                }
                for (int a = 1; a < 8; a++)
                {
                    SendPacket s = new SendPacket();
                    s.PackArray(new byte[] { 39, 27 });
                    s.Pack8((byte)a);
                    s.Pack32(0);
                    src.Send(s);
                }
            }
        }

        public void GetMemberOnline(Player src)
        {
            foreach (var pair in MembersOnline.ToList())
            {
                SendPacket s = new SendPacket();
                s.PackArray(new byte[] { 39, 60 });
                s.Pack32(pair.Value.ID);
                s.PackStringN(pair.Value.CharacterName);
                s.Pack8(pair.Value.Level);
                s.Pack8((byte)pair.Value.Job);
                s.Pack8(pair.Value.Reborn ? (byte)1 : (byte)0);
                s.Pack8((byte)pair.Value.Element);
                s.Pack8((byte)pair.Value.Body);
                s.Pack8(pair.Value.Head);
                s.Pack16(pair.Value.HairColor);
                s.Pack16(pair.Value.SkinColor);
                s.Pack16(pair.Value.ClothingColor);
                s.Pack16(pair.Value.EyeColor);
                s.PackStringN(pair.Value.Nickname);
                src.Send(s);
            }
        }

        public void GetMemberState(Player src)
        {
            foreach (var pair in MembersOnline.ToList())
            {
                SendPacket s = new SendPacket();
                s.PackArray(new byte[] { 39, 61 });
                s.Pack32(pair.Value.ID);
                s.Pack8(0); // 0 = online, 1 = busy
                src.Send(s);
            }
        }

        void GetInsigneGuild(Player src)
        {
            SendPacket s = new SendPacket();
            s.PackArray(new byte[] { 39, 30, 2 });
            s.PackArray(ImgInsigne.ToArray());
            src.Send(s);
        }

        void GetGuilNickName(Player src)
        {
            SendPacket s = new SendPacket();
            s.PackArray(new byte[] { 39, 9 });
            s.Pack32(src.CharID);
            s.Pack32(IconGuil);
            s.PackStringN(GuildName);
            if (src.CurMap is GameMap)
                ((GameMap)src.CurMap).Broadcast(s);
            else
                src.Send(s);
        }

        #endregion

        void BroadCastGuild(SendPacket spk, uint exceptID)
        {
            foreach (var pair in MembersOnline)
            {
                if (pair.Value.ID != exceptID && pair.Value.isOnline)
                {
                    ((Player)pair.Value.OnlineSrc).Send(spk);
                }
            }
        }

        #region Global SendPacket

        void Send39_8(Player src)
        {
            SendPacket s = new SendPacket();
            s.PackArray(new byte[] { 39, 8 });
            s.Pack32(src.CharID);
            BroadCastGuild(s, src.CharID);
        }

        void Send39_61(Player src)
        {
            SendPacket s = new SendPacket();
            s.PackArray(new byte[] { 39, 61 });
            s.Pack32(src.CharID);
            s.Pack8(0); // 0 = on, 1 = busy, 3 = offline
            BroadCastGuild(s, src.CharID);
        }

        void Send39_60(Player src)
        {
            SendPacket s = new SendPacket();
            s.PackArray(new byte[] { 39, 60 });
            s.Pack32(src.CharID);
            s.PackStringN(src.CharName);
            s.Pack8(src.Level);
            s.Pack8((byte)src.Job);
            s.Pack8(src.Reborn ? (byte)1 : (byte)0);
            s.Pack8((byte)src.Element);
            s.Pack8((byte)src.Body);
            s.Pack8(src.Head);
            s.Pack16(src.HairColor);
            s.Pack16(src.SkinColor);
            s.Pack16(src.ClothingColor);
            s.Pack16(src.EyeColor);
            s.PackStringN(src.NickName);
            BroadCastGuild(s, src.CharID);
        }

        #endregion

        #region Methods Guild

        public void ChangInsigneGuild(Player src, RecievePacket r)
        {
            // TODO: verify permission
        }

        public void Edit_Rule(string text)
        {
            Rules = text;
            SendPacket s = new SendPacket();
            s.PackArray(new byte[] { 39, 11 });
            s.PackStringN(text);
            BroadCastGuild(s, 0);
        }

        public void Dismiss(uint target, uint actor)
        {
            var tmp = Members.Find(c => c.ID == target);
            if (tmp != null)
            {
                SendPacket s = new SendPacket();
                s.PackArray(new byte[] { 39, 6 });
                s.Pack32(target);
                BroadCastGuild(s, target);

                s = new SendPacket();
                s.PackArray(new byte[] { 39, 7, 0 });
                s.Pack32(target);

                // Send to actor
                var actorMem = Members.Find(c => c.ID == actor);
                if (actorMem != null && actorMem.isOnline)
                    ((Player)actorMem.OnlineSrc).Send(s);

                // Send to target
                if (tmp.isOnline)
                {
                    ((Player)tmp.OnlineSrc).Send(s);
                    ((Player)tmp.OnlineSrc).CurGuild = null;
                }

                Members.Remove(tmp);
            }
        }

        public void LeaveGuild(Player src)
        {
            uint id = src.CharID;
            var tmp = Members.Find(c => c.ID == id);

            SendPacket s = new SendPacket();
            s.PackArray(new byte[] { 39, 7, 0 });
            s.Pack32(src.CharID);
            src.Send(s);

            s = new SendPacket();
            s.PackArray(new byte[] { 39, 6 });
            s.Pack32(src.CharID);
            BroadCastGuild(s, src.CharID);

            s = new SendPacket();
            s.PackArray(new byte[] { 39, 9 });
            s.Pack32(src.CharID);
            s.PackArray(new byte[] { 0, 0, 0, 0, 0 });
            if (src.CurMap is GameMap)
                ((GameMap)src.CurMap).Broadcast(s);

            src.CurGuild = null;
            Members.Remove(tmp);
        }

        #endregion

        #region Guild Warehouse

        public int WarehouseCount { get { return Warehouse.Count; } }

        /// <summary>
        /// Deposit an item from inventory into the guild warehouse.
        /// </summary>
        public bool DepositItem(Player src, byte invSlot)
        {
            if (Warehouse.Count >= WAREHOUSE_MAX) return false;

            var item = src.Inv[invSlot];
            if (item == null || item.ItemID == 0) return false;

            Item stored = new Item();
            stored.CopyFrom(item);
            Warehouse.Add(stored);
            src.Inv.RemoveItem(invSlot, 1);

            // Notify AC 39,40 — item deposited
            SendPacket pkt = new SendPacket();
            pkt.Pack8(39);
            pkt.Pack8(40);
            pkt.Pack8((byte)(Warehouse.Count - 1));
            pkt.Pack16(stored.ItemID);
            pkt.Pack32(src.CharID);
            BroadCastGuild(pkt, 0);

            return true;
        }

        /// <summary>
        /// Withdraw an item from the guild warehouse to inventory.
        /// </summary>
        public bool WithdrawItem(Player src, byte warehouseSlot)
        {
            if (warehouseSlot >= Warehouse.Count) return false;

            var item = Warehouse[warehouseSlot];
            if (item == null || item.ItemID == 0) return false;

            src.Inv.AddItem(item.ItemID, 1);
            Warehouse.RemoveAt(warehouseSlot);

            // Notify AC 39,41 — item withdrawn
            SendPacket pkt = new SendPacket();
            pkt.Pack8(39);
            pkt.Pack8(41);
            pkt.Pack8(warehouseSlot);
            pkt.Pack32(src.CharID);
            BroadCastGuild(pkt, 0);

            return true;
        }

        /// <summary>
        /// Send warehouse contents to player.
        /// </summary>
        public void SendWarehouseList(Player src)
        {
            SendPacket pkt = new SendPacket();
            pkt.Pack8(39);
            pkt.Pack8(42);
            pkt.Pack8((byte)Warehouse.Count);

            for (int i = 0; i < Warehouse.Count; i++)
            {
                pkt.Pack8((byte)i);
                pkt.Pack16(Warehouse[i].ItemID);
            }

            src.Send(pkt);
        }

        #endregion

        #region Message Guild

        public void AddMessage(Player src, RecievePacket r)
        {
            OpenTab(src);
        }

        public void OpenTab(Player src)
        {
            // Guild message board — not yet implemented
        }

        public void OpenPainelWriteMessage(Player src)
        {
            SendPacket s = new SendPacket();
            s.PackArray(new byte[] { 82, 8, 2 });
            src.Send(s);
        }

        public void OpenMessage(Player src, RecievePacket r)
        {
            // Not yet implemented
        }

        #endregion
    }
}
