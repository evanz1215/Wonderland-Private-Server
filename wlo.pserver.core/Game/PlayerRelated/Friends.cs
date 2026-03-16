using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network;

namespace Game
{
    /// <summary>
    /// Manages a player's friend list.
    /// Stores friend CharIDs and handles add/remove/online notifications.
    ///
    /// AC 14 protocol:
    /// Sub 1: Friend request (client→server: targetCharID)
    /// Sub 2: Accept friend request (client→server: requesterCharID)
    /// Sub 3: Remove friend (client→server: friendCharID)
    /// Sub 4: Friend removed notification (server→client)
    /// Sub 5: Full friend list (server→client, sent on login)
    /// Sub 7: Friend online/offline notification (server→client)
    /// Sub 9: Friend added notification (server→client)
    /// </summary>
    public class FriendManager
    {
        Player m_owner;
        List<uint> m_friendIDs;
        const int MAX_FRIENDS = 50;

        /// <summary>
        /// Set by the main project to enable global player lookup.
        /// Returns a Player by CharID if online, or null.
        /// </summary>
        public static Func<uint, Player> GlobalFindPlayer;

        public FriendManager(Player owner)
        {
            m_owner = owner;
            m_friendIDs = new List<uint>();
        }

        public List<uint> FriendIDs { get { return m_friendIDs; } }
        public int Count { get { return m_friendIDs.Count; } }

        /// <summary>
        /// Send a friend request to target player.
        /// Server sends AC 14,1 to target with requester's CharID.
        /// </summary>
        public void RequestFriend(Player target)
        {
            if (m_friendIDs.Count >= MAX_FRIENDS)
                return;
            if (m_friendIDs.Contains(target.CharID))
                return;
            if (target.Friends.m_friendIDs.Count >= MAX_FRIENDS)
                return;

            // Send request to target
            SendPacket pkt = new SendPacket();
            pkt.Pack8(14);
            pkt.Pack8(1);
            pkt.Pack32(m_owner.CharID);
            pkt.PackStringN(m_owner.CharName);
            target.Send(pkt);
        }

        /// <summary>
        /// Accept a friend request. Adds both players as friends of each other.
        /// </summary>
        public void AcceptFriend(Player requester)
        {
            if (m_friendIDs.Contains(requester.CharID))
                return;

            // Add each other
            AddFriend(requester.CharID);
            requester.Friends.AddFriend(m_owner.CharID);

            // Notify both: AC 14,9
            SendAddedNotification(m_owner, requester);
            SendAddedNotification(requester, m_owner);
        }

        /// <summary>
        /// Remove a friend by CharID.
        /// </summary>
        public void RemoveFriend(uint friendCharID)
        {
            if (!m_friendIDs.Contains(friendCharID))
                return;

            m_friendIDs.Remove(friendCharID);

            // Notify owner: AC 14,4
            SendPacket pkt = new SendPacket();
            pkt.Pack8(14);
            pkt.Pack8(4);
            pkt.Pack32(friendCharID);
            m_owner.Send(pkt);
        }

        /// <summary>
        /// Send the full friend list to the owner on login.
        /// AC 14,5 with each friend's basic data.
        /// </summary>
        public void SendFriendList()
        {
            SendPacket pkt = new SendPacket();
            pkt.Pack8(14);
            pkt.Pack8(5);

            foreach (uint friendID in m_friendIDs)
            {
                pkt.Pack32(friendID);
                // Send minimal data — client can request full details if needed
                pkt.Pack8(0); // online status placeholder
            }

            m_owner.Send(pkt);
        }

        /// <summary>
        /// Notify all online friends that this player has come online.
        /// AC 14,7 with CharID + online status.
        /// </summary>
        public void NotifyFriendsOnline()
        {
            SendPacket pkt = new SendPacket();
            pkt.Pack8(14);
            pkt.Pack8(7);
            pkt.Pack32(m_owner.CharID);
            pkt.Pack8(1); // 1 = online

            SendToOnlineFriends(pkt);
        }

        /// <summary>
        /// Notify all online friends that this player has gone offline.
        /// </summary>
        public void NotifyFriendsOffline()
        {
            SendPacket pkt = new SendPacket();
            pkt.Pack8(14);
            pkt.Pack8(7);
            pkt.Pack32(m_owner.CharID);
            pkt.Pack8(0); // 0 = offline

            SendToOnlineFriends(pkt);
        }

        /// <summary>
        /// Load friend list from database string (CharIDs separated by '&')
        /// </summary>
        public void LoadFromDB(string data)
        {
            m_friendIDs.Clear();
            if (string.IsNullOrEmpty(data) || data == "none")
                return;

            string[] parts = data.Split('&');
            foreach (string part in parts)
            {
                uint id;
                if (uint.TryParse(part.Trim(), out id) && id > 0)
                    m_friendIDs.Add(id);
            }
        }

        /// <summary>
        /// Save friend list to database string format
        /// </summary>
        public string SaveToDB()
        {
            if (m_friendIDs.Count == 0)
                return "none";

            return string.Join("&", m_friendIDs.Select(id => id.ToString()));
        }

        void AddFriend(uint charID)
        {
            if (!m_friendIDs.Contains(charID) && m_friendIDs.Count < MAX_FRIENDS)
                m_friendIDs.Add(charID);
        }

        void SendAddedNotification(Player target, Player newFriend)
        {
            SendPacket pkt = new SendPacket();
            pkt.Pack8(14);
            pkt.Pack8(9);
            pkt.Pack32(newFriend.CharID);
            pkt.PackStringN(newFriend.CharName);
            pkt.Pack8((byte)newFriend.Level);
            target.Send(pkt);
        }

        void SendToOnlineFriends(SendPacket pkt)
        {
            if (GlobalFindPlayer == null) return;

            foreach (uint friendID in m_friendIDs)
            {
                Player friend = GlobalFindPlayer(friendID);
                if (friend != null)
                    friend.Send(pkt);
            }
        }
    }
}
