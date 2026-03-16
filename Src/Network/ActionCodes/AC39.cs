using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Maps;
using Network;

namespace Network.ActionCodes
{
    /// <summary>
    /// AC 39 — Guild System
    /// Sub 2: Invite member
    /// Sub 3: Accept invite
    /// Sub 4: Guild mail (stub)
    /// Sub 6: Leave guild
    /// Sub 7: Dismiss member
    /// Sub 8: Tab message
    /// Sub 9: Edit rules
    /// Sub 11: Remove vice leader
    /// Sub 14: Appoint vice leader
    /// Sub 16: Change permissions
    /// Sub 18: Change insignia
    /// Sub 20: Deposit to warehouse
    /// Sub 21: Withdraw from warehouse
    /// Sub 22: View warehouse
    /// </summary>
    public class AC39_Guild : AC
    {
        public override int ID { get { return 39; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                case 2: Recv_Invite(c, p); break;
                case 3: Recv_AcceptInvite(c, p); break;
                case 4: Recv_GuildMail(c, p); break;
                case 6: Recv_LeaveGuild(c, p); break;
                case 7: Recv_DismissMember(c, p); break;
                case 8: Recv_TabMessage(c, p); break;
                case 9: Recv_EditRule(c, p); break;
                case 11: Recv_RemoveViceLeader(c, p); break;
                case 14: Recv_AppointViceLeader(c, p); break;
                case 16: Recv_ChangePermission(c, p); break;
                case 18: Recv_ChangeInsignia(c, p); break;
                case 20: Recv_WarehouseDeposit(c, p); break;
                case 21: Recv_WarehouseWithdraw(c, p); break;
                case 22: Recv_WarehouseView(c, p); break;
                default: DebugSystem.Write("AC 39," + p.B + " has not been coded"); break;
            }
        }

        /// <summary>
        /// Invite a player on the same map to the guild.
        /// Packet: [39][2][targetCharID:uint]
        /// </summary>
        void Recv_Invite(Player r, RecievePacket p)
        {
            try
            {
                if (r.CurGuild == null) return;

                p.SetPtr(6);
                uint targetID = p.Unpack32();

                if (r.CurMap is GameMap)
                {
                    var target = ((GameMap)r.CurMap).FindPlayer(targetID);
                    if (target != null)
                    {
                        SendPacket s = new SendPacket();
                        s.PackArray(new byte[] { 39, 3 });
                        s.Pack32(r.CharID);
                        target.Send(s);
                    }
                }
            }
            catch (Exception t) { DebugSystem.Write(t.ToString()); }
        }

        /// <summary>
        /// Accept guild invitation.
        /// Packet: [39][3][inviterCharID:uint]
        /// </summary>
        void Recv_AcceptInvite(Player r, RecievePacket p)
        {
            try
            {
                p.SetPtr(6);
                uint inviterID = p.Unpack32();

                if (r.CurMap is GameMap)
                {
                    var inviter = ((GameMap)r.CurMap).FindPlayer(inviterID);
                    if (inviter != null && inviter.CurGuild != null)
                    {
                        inviter.CurGuild.AddNewMemberGuild(r, inviterID);
                    }
                }
            }
            catch (Exception t) { DebugSystem.Write(t.ToString()); }
        }

        /// <summary>
        /// Guild mail (stub).
        /// </summary>
        void Recv_GuildMail(Player r, RecievePacket p)
        {
            // Not yet implemented
        }

        /// <summary>
        /// Leave current guild.
        /// Packet: [39][6]
        /// </summary>
        void Recv_LeaveGuild(Player r, RecievePacket p)
        {
            try
            {
                if (r.CurGuild == null) return;
                r.CurGuild.LeaveGuild(r);
            }
            catch (Exception t) { DebugSystem.Write(t.ToString()); }
        }

        /// <summary>
        /// Dismiss a member (leader only).
        /// Packet: [39][7][targetCharID:uint]
        /// </summary>
        void Recv_DismissMember(Player r, RecievePacket p)
        {
            try
            {
                if (r.CurGuild == null) return;
                if (r.CurGuild.Leader.ID != r.CharID) return;

                p.SetPtr(6);
                uint target = p.Unpack32();
                r.CurGuild.Dismiss(target, r.CharID);
            }
            catch (Exception t) { DebugSystem.Write(t.ToString()); }
        }

        /// <summary>
        /// Tab message (stub).
        /// </summary>
        void Recv_TabMessage(Player r, RecievePacket p)
        {
            // Not yet implemented
        }

        /// <summary>
        /// Edit guild rules (leader only).
        /// Packet: [39][9][ruleText:stringN]
        /// </summary>
        void Recv_EditRule(Player r, RecievePacket p)
        {
            try
            {
                if (r.CurGuild == null) return;
                if (r.CurGuild.Leader.ID != r.CharID) return;

                p.SetPtr(6);
                string text = p.UnpackStringN();
                r.CurGuild.Edit_Rule(text);
            }
            catch (Exception t) { DebugSystem.Write(t.ToString()); }
        }

        /// <summary>
        /// Remove a vice leader (leader only).
        /// Packet: [39][11][targetCharID:uint]
        /// </summary>
        void Recv_RemoveViceLeader(Player r, RecievePacket p)
        {
            try
            {
                if (r.CurGuild == null) return;
                if (r.CurGuild.Leader.ID != r.CharID) return;

                p.SetPtr(6);
                uint target = p.Unpack32();
                r.CurGuild.RemoveHoldThePostOfViceOrgleader(r, target);
            }
            catch (Exception t) { DebugSystem.Write(t.ToString()); }
        }

        /// <summary>
        /// Appoint a vice leader (leader only).
        /// Packet: [39][14][targetCharID:uint][type:byte]
        /// </summary>
        void Recv_AppointViceLeader(Player r, RecievePacket p)
        {
            try
            {
                if (r.CurGuild == null) return;
                if (r.CurGuild.Leader.ID != r.CharID) return;

                p.SetPtr(6);
                uint target = p.Unpack32();
                byte type = p.Unpack8();
                r.CurGuild.HoldThePostOfViceOrgleader(target, type);
            }
            catch (Exception t) { DebugSystem.Write(t.ToString()); }
        }

        /// <summary>
        /// Change member permissions (leader only).
        /// Packet: [39][16][targetCharID:uint][permData...]
        /// </summary>
        void Recv_ChangePermission(Player r, RecievePacket p)
        {
            try
            {
                if (r.CurGuild == null) return;
                if (r.CurGuild.Leader.ID != r.CharID) return;

                p.SetPtr(6);
                uint target = p.Unpack32();
                r.CurGuild.ChangePermissionMember(target, p);
            }
            catch (Exception t) { DebugSystem.Write(t.ToString()); }
        }

        /// <summary>
        /// Change guild insignia (leader only).
        /// </summary>
        void Recv_ChangeInsignia(Player r, RecievePacket p)
        {
            try
            {
                if (r.CurGuild == null) return;
                if (r.CurGuild.Leader.ID != r.CharID) return;
                r.CurGuild.ChangInsigneGuild(r, p);
            }
            catch (Exception t) { DebugSystem.Write(t.ToString()); }
        }

        /// <summary>
        /// Deposit item into guild warehouse.
        /// Packet: [39][20][invSlot:byte]
        /// </summary>
        void Recv_WarehouseDeposit(Player r, RecievePacket p)
        {
            if (r.CurGuild == null) return;

            p.SetPtr(6);
            byte invSlot = p.Unpack8();
            r.CurGuild.DepositItem(r, invSlot);
        }

        /// <summary>
        /// Withdraw item from guild warehouse.
        /// Packet: [39][21][warehouseSlot:byte]
        /// </summary>
        void Recv_WarehouseWithdraw(Player r, RecievePacket p)
        {
            if (r.CurGuild == null) return;

            p.SetPtr(6);
            byte slot = p.Unpack8();
            r.CurGuild.WithdrawItem(r, slot);
        }

        /// <summary>
        /// View guild warehouse contents.
        /// Packet: [39][22]
        /// </summary>
        void Recv_WarehouseView(Player r, RecievePacket p)
        {
            if (r.CurGuild == null) return;
            r.CurGuild.SendWarehouseList(r);
        }
    }
}
