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
    /// AC 13 — Team/Party Actions
    /// Sub 1: Request to join a team (client → server)
    /// Sub 2: Accept/decline join request
    /// Sub 4: Leave team
    /// Sub 9: Invite to team
    /// Sub 10: Accept/decline invite
    /// Sub 15: Transfer leadership
    /// Sub 17: Kick member
    /// </summary>
    public class AC13_Team : AC
    {
        public override int ID { get { return 13; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                case 1: Recv_JoinRequest(c, p); break;
                case 2: Recv_AcceptJoin(c, p); break;
                case 4: Recv_LeaveTeam(c, p); break;
                case 9: Recv_InviteToTeam(c, p); break;
                case 10: Recv_AcceptInvite(c, p); break;
                case 15: Recv_TransferLeader(c, p); break;
                case 17: Recv_KickMember(c, p); break;
                default: DebugSystem.Write("AC 13," + p.B + " has not been coded"); break;
            }
        }

        /// <summary>
        /// Player requests to join another player's team
        /// Packet: [13][1][targetCharID:uint32]
        /// </summary>
        void Recv_JoinRequest(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            uint targetID = p.Unpack32();

            Player target = FindPlayerByID(r, targetID);
            if (target == null) return;

            r.Team.RequestJoin(target);
        }

        /// <summary>
        /// Leader accepts/declines a join request
        /// Packet: [13][2][answer:byte][requesterCharID:uint32]
        /// answer: 1 = accept, 0 = decline
        /// </summary>
        void Recv_AcceptJoin(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte answer = p.Unpack8();
            uint requesterID = p.Unpack32();

            if (answer != 1) return;

            Player requester = FindPlayerByID(r, requesterID);
            if (requester == null) return;

            r.Team.AcceptMember(requester);
        }

        /// <summary>
        /// Player leaves their current team
        /// Packet: [13][4]
        /// </summary>
        void Recv_LeaveTeam(Player r, RecievePacket p)
        {
            r.Team.LeaveTeam();
        }

        /// <summary>
        /// Leader invites a player to the team
        /// Packet: [13][9][targetCharID:uint32]
        /// </summary>
        void Recv_InviteToTeam(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            uint targetID = p.Unpack32();

            Player target = FindPlayerByID(r, targetID);
            if (target == null) return;

            r.Team.InviteToTeam(target);
        }

        /// <summary>
        /// Player accepts/declines a team invite
        /// Packet: [13][10][answer:byte][inviterCharID:uint32]
        /// </summary>
        void Recv_AcceptInvite(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte answer = p.Unpack8();
            uint inviterID = p.Unpack32();

            if (answer != 1) return;

            Player inviter = FindPlayerByID(r, inviterID);
            if (inviter == null) return;

            inviter.Team.AcceptMember(r);
        }

        /// <summary>
        /// Leader transfers leadership
        /// Packet: [13][15][targetCharID:uint32]
        /// </summary>
        void Recv_TransferLeader(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            uint targetID = p.Unpack32();

            Player target = FindPlayerByID(r, targetID);
            if (target == null) return;

            r.Team.TransferLeader(target);
        }

        /// <summary>
        /// Leader kicks a member
        /// Packet: [13][17][targetCharID:uint32]
        /// </summary>
        void Recv_KickMember(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            uint targetID = p.Unpack32();

            Player target = FindPlayerByID(r, targetID);
            if (target == null) return;

            r.Team.KickMember(target);
        }

        /// <summary>
        /// Find a player by CharID in the same map
        /// </summary>
        Player FindPlayerByID(Player src, uint charID)
        {
            if (src.CurMap == null) return null;

            // Search through players on the same map
            var map = src.CurMap as GameMap;
            if (map == null) return null;

            return map.FindPlayer(charID);
        }
    }
}
