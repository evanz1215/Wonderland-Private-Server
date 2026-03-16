using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Network;

namespace Network.ActionCodes
{
    /// <summary>
    /// AC 85 — Instance / Dungeon Party System
    /// Sub 1: Update instance list
    /// Sub 2: Tab instance list
    /// Sub 3: Create instance
    /// Sub 4: Pre-join (preview members)
    /// Sub 5: Join instance
    /// Sub 6: Exit instance
    /// Sub 10: Check members
    /// Sub 11: Check members (tab)
    /// Sub 13: Dismiss member
    /// </summary>
    public class AC85_Instance : AC
    {
        /// <summary>
        /// Static reference to the global InstanceSystem.
        /// Set during WorldServer.Initialize().
        /// </summary>
        public static InstanceSystem Instances;

        public override int ID { get { return 85; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            if (Instances == null) return;

            switch (p.B)
            {
                case 1: Recv_UpdateList(c, p); break;
                case 2: Recv_TabList(c, p); break;
                case 3: Recv_Create(c, p); break;
                case 4: Recv_PreJoin(c, p); break;
                case 5: Recv_Join(c, p); break;
                case 6: Recv_Exit(c, p); break;
                case 10: Recv_CheckMembers(c, p); break;
                case 11: Recv_CheckMembersTab(c, p); break;
                case 13: Recv_Dismiss(c, p); break;
                default: DebugSystem.Write("AC 85," + p.B + " has not been coded"); break;
            }
        }

        /// <summary>
        /// Update/refresh instance list (default tab 1).
        /// Packet: [85][1]
        /// </summary>
        void Recv_UpdateList(Player r, RecievePacket p)
        {
            Instances.SendInstanceList(r, 1);
        }

        /// <summary>
        /// Switch tab in instance list.
        /// Packet: [85][2][tab:byte]
        /// </summary>
        void Recv_TabList(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte tab = p.Unpack8();
            if (tab < 1 || tab > 4) tab = 1;
            Instances.SendInstanceList(r, tab);
        }

        /// <summary>
        /// Create a new instance.
        /// Packet: [85][3][dataID:ushort][text:stringN]
        /// </summary>
        void Recv_Create(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            ushort dataID = p.Unpack16();
            string text = p.UnpackStringN();
            Instances.CreateInstance(r, dataID, text);
        }

        /// <summary>
        /// Preview instance members before joining.
        /// Packet: [85][4][instanceID:ushort]
        /// </summary>
        void Recv_PreJoin(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            ushort instanceID = p.Unpack16();
            Instances.PreJoin(r, instanceID);
        }

        /// <summary>
        /// Join an existing instance.
        /// Packet: [85][5][instanceID:ushort]
        /// </summary>
        void Recv_Join(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            ushort instanceID = p.Unpack16();
            Instances.JoinInstance(r, instanceID);
        }

        /// <summary>
        /// Exit current instance.
        /// Packet: [85][6]
        /// </summary>
        void Recv_Exit(Player r, RecievePacket p)
        {
            Instances.ExitInstance(r);
        }

        /// <summary>
        /// Check member list (default tab 1).
        /// Packet: [85][10]
        /// </summary>
        void Recv_CheckMembers(Player r, RecievePacket p)
        {
            if (r.CurInstance != 0)
                Instances.CheckMembers(r, 1);
        }

        /// <summary>
        /// Check member list with tab.
        /// Packet: [85][11][tab:byte]
        /// </summary>
        void Recv_CheckMembersTab(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte tab = p.Unpack8();
            if (r.CurInstance != 0 && tab >= 1 && tab <= 4)
                Instances.CheckMembers(r, tab);
        }

        /// <summary>
        /// Dismiss a member (creator only).
        /// Packet: [85][13][memberCharID:uint]
        /// </summary>
        void Recv_Dismiss(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            uint memberID = p.Unpack32();
            Instances.DismissMember(r, memberID);
        }
    }
}
