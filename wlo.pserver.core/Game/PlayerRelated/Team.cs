using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network;

namespace Game
{
    /// <summary>
    /// Manages a player's team/party state.
    ///
    /// WLO Team Protocol:
    ///   AC 13,1  → Join team request (player → target)
    ///   AC 13,3  → Accept member (confirmation to requester)
    ///   AC 13,4  → Player left team (broadcast)
    ///   AC 13,5  → Member join confirmation (broadcast)
    ///   AC 13,6  → Full team data packet
    ///   AC 13,9  → Invite to team (leader → target)
    ///   AC 13,10 → Accept invite response
    ///   AC 13,15 → Leader change
    /// </summary>
    public class TeamManager
    {
        Player m_owner;
        List<Player> m_members;
        bool m_isLeader;
        readonly object m_lock = new object();

        public TeamManager(Player owner)
        {
            m_owner = owner;
            m_members = new List<Player>();
            m_isLeader = false;
        }

        public bool IsLeader
        {
            get { lock (m_lock) return m_isLeader; }
            set { lock (m_lock) m_isLeader = value; }
        }

        public bool HasParty
        {
            get { lock (m_lock) return m_members.Count > 0; }
        }

        public List<Player> TeamMembers
        {
            get { lock (m_lock) return m_members.ToList(); }
        }

        public int MemberCount
        {
            get { lock (m_lock) return m_members.Count + 1; } // +1 for self
        }

        /// <summary>
        /// Send a join request to another player (AC 13,1)
        /// </summary>
        public void RequestJoin(Player target)
        {
            if (target == null || target.CharID == m_owner.CharID) return;

            SendPacket p = new SendPacket();
            p.Pack8(13);
            p.Pack8(1);
            p.Pack32(m_owner.CharID);
            target.Send(p);
        }

        /// <summary>
        /// Send a team invite to another player (AC 13,9)
        /// </summary>
        public void InviteToTeam(Player target)
        {
            if (target == null || target.CharID == m_owner.CharID) return;

            SendPacket p = new SendPacket();
            p.Pack8(13);
            p.Pack8(9);
            p.Pack32(m_owner.CharID);
            target.Send(p);
        }

        /// <summary>
        /// Accept a member into the team.
        /// Called on the leader when they accept a join request.
        /// </summary>
        public void AcceptMember(Player joiner)
        {
            if (joiner == null || joiner.CharID == m_owner.CharID) return;
            lock (m_lock)
            {
                if (m_members.Any(c => c.CharID == joiner.CharID)) return;
                if (m_members.Count >= 4) return; // max 5 members total (leader + 4)

                // If not already a leader, become one
                if (!m_isLeader && m_members.Count == 0)
                    m_isLeader = true;

                // Send accept to joiner (AC 13,3)
                SendPacket accept = new SendPacket();
                accept.Pack8(13);
                accept.Pack8(3);
                accept.Pack8(1); // success
                accept.Pack32(m_owner.CharID);
                joiner.Send(accept);

                // Add joiner to the team
                joiner.Team.AddMember(m_owner);
                foreach (var member in m_members)
                {
                    joiner.Team.AddMember(member);
                    member.Team.AddMember(joiner);
                }
                m_members.Add(joiner);

                // Broadcast join (AC 13,5)
                SendPacket joinPkt = new SendPacket();
                joinPkt.Pack8(13);
                joinPkt.Pack8(5);
                joinPkt.Pack32(m_owner.CharID);
                joinPkt.Pack32(joiner.CharID);

                m_owner.Send(joinPkt);
                foreach (var member in m_members)
                    member.Send(joinPkt);
            }
        }

        /// <summary>
        /// Add a member reference (called on non-leader members).
        /// </summary>
        internal void AddMember(Player p)
        {
            lock (m_lock)
            {
                if (!m_members.Any(c => c.CharID == p.CharID))
                    m_members.Add(p);
            }
        }

        /// <summary>
        /// Remove a member from this player's team list.
        /// </summary>
        internal void RemoveMember(Player p)
        {
            lock (m_lock)
            {
                m_members.RemoveAll(c => c.CharID == p.CharID);
            }
        }

        /// <summary>
        /// Leave the current team.
        /// </summary>
        public void LeaveTeam()
        {
            lock (m_lock)
            {
                if (m_isLeader)
                {
                    // Leader leaving = disband team
                    DisbandTeam();
                    return;
                }

                // Notify all members (AC 13,4)
                SendPacket leavePkt = new SendPacket();
                leavePkt.Pack8(13);
                leavePkt.Pack8(4);
                leavePkt.Pack32(m_owner.CharID);

                foreach (var member in m_members)
                {
                    member.Team.RemoveMember(m_owner);
                    member.Send(leavePkt);
                }

                m_owner.Send(leavePkt);
                m_members.Clear();
                m_isLeader = false;
            }
        }

        /// <summary>
        /// Kick a member from the team (leader only).
        /// </summary>
        public void KickMember(Player target)
        {
            lock (m_lock)
            {
                if (!m_isLeader || target == null) return;
                if (!m_members.Any(c => c.CharID == target.CharID)) return;

                // Remove from everyone's list
                target.Team.LeaveTeam();
            }
        }

        /// <summary>
        /// Disband the entire team (leader action).
        /// </summary>
        public void DisbandTeam()
        {
            lock (m_lock)
            {
                foreach (var member in m_members.ToList())
                {
                    // Notify each member
                    SendPacket leavePkt = new SendPacket();
                    leavePkt.Pack8(13);
                    leavePkt.Pack8(4);
                    leavePkt.Pack32(m_owner.CharID);
                    member.Send(leavePkt);
                    member.Team.RemoveMember(m_owner);

                    // Clear their team lists too
                    foreach (var other in m_members)
                    {
                        if (other.CharID != member.CharID)
                            member.Team.RemoveMember(other);
                    }
                    member.Team.IsLeader = false;
                }

                // Notify self
                SendPacket selfLeave = new SendPacket();
                selfLeave.Pack8(13);
                selfLeave.Pack8(4);
                selfLeave.Pack32(m_owner.CharID);
                m_owner.Send(selfLeave);

                m_members.Clear();
                m_isLeader = false;
            }
        }

        /// <summary>
        /// Transfer leadership to another member (AC 13,15).
        /// </summary>
        public void TransferLeader(Player newLeader)
        {
            lock (m_lock)
            {
                if (!m_isLeader || newLeader == null) return;
                if (!m_members.Any(c => c.CharID == newLeader.CharID)) return;

                m_isLeader = false;
                newLeader.Team.IsLeader = true;

                // Broadcast leader change (AC 13,15)
                SendPacket p = new SendPacket();
                p.Pack8(13);
                p.Pack8(15);
                p.Pack8(3);
                p.Pack32(newLeader.CharID);
                p.Pack32(m_owner.CharID);

                m_owner.Send(p);
                foreach (var member in m_members)
                    member.Send(p);
            }
        }

        /// <summary>
        /// Get the team data packet (AC 13,6) for broadcasting to other players.
        /// </summary>
        public SendPacket GetTeamDataPacket()
        {
            lock (m_lock)
            {
                SendPacket f = new SendPacket();
                f.Pack8(13);
                f.Pack8(6);
                f.Pack32(m_owner.CharID);
                f.Pack8((byte)m_members.Count);
                foreach (Player y in m_members)
                    f.Pack32(y.CharID);
                return f;
            }
        }
    }
}
