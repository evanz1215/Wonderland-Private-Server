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
    /// AC 14 — Friend &amp; Mail System
    /// Friends:
    ///   Sub 1: Send friend request (client → server)
    ///   Sub 2: Accept friend request
    ///   Sub 3: Remove friend
    /// Mail:
    ///   Sub 10: Send mail (client → server)
    ///   Sub 11: Request mail list
    ///   Sub 12: Delete mail
    ///   Sub 13: Take mail attachment
    /// </summary>
    public class AC14_Friends : AC
    {
        public override int ID { get { return 14; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                // Friends
                case 1: Recv_FriendRequest(c, p); break;
                case 2: Recv_AcceptFriend(c, p); break;
                case 3: Recv_RemoveFriend(c, p); break;
                // Mail
                case 10: Recv_SendMail(c, p); break;
                case 11: Recv_MailList(c, p); break;
                case 12: Recv_DeleteMail(c, p); break;
                case 13: Recv_TakeAttachment(c, p); break;
                default: DebugSystem.Write("AC 14," + p.B + " has not been coded"); break;
            }
        }

        #region Friends

        /// <summary>
        /// Player sends a friend request to another player
        /// Packet: [14][1][targetCharID:uint32]
        /// </summary>
        void Recv_FriendRequest(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            uint targetID = p.Unpack32();

            Player target = cGlobal.gWorld.FindPlayerByCharID(targetID);
            if (target == null) return;

            r.Friends.RequestFriend(target);
        }

        /// <summary>
        /// Player accepts a friend request
        /// Packet: [14][2][requesterCharID:uint32]
        /// </summary>
        void Recv_AcceptFriend(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            uint requesterID = p.Unpack32();

            Player requester = cGlobal.gWorld.FindPlayerByCharID(requesterID);
            if (requester == null) return;

            r.Friends.AcceptFriend(requester);
        }

        /// <summary>
        /// Player removes a friend
        /// Packet: [14][3][friendCharID:uint32]
        /// </summary>
        void Recv_RemoveFriend(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            uint friendID = p.Unpack32();

            r.Friends.RemoveFriend(friendID);
        }

        #endregion

        #region Mail

        /// <summary>
        /// Player sends mail to another player
        /// Packet: [14][10][targetCharID:uint32][message:stringN][itemID:uint16][itemAmount:byte]
        /// </summary>
        void Recv_SendMail(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            uint targetID = p.Unpack32();
            string message = p.UnpackStringN();
            ushort itemID = p.Unpack16();
            byte itemAmount = p.Unpack8();

            r.Mail.SendMail(targetID, message, itemID, itemAmount);
        }

        /// <summary>
        /// Player requests their mail list
        /// Packet: [14][11]
        /// </summary>
        void Recv_MailList(Player r, RecievePacket p)
        {
            r.Mail.SendMailList();
        }

        /// <summary>
        /// Player deletes a mail
        /// Packet: [14][12][mailIndex:byte]
        /// </summary>
        void Recv_DeleteMail(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte index = p.Unpack8();

            r.Mail.DeleteMail(index);
        }

        /// <summary>
        /// Player takes item attachment from mail
        /// Packet: [14][13][mailIndex:byte]
        /// </summary>
        void Recv_TakeAttachment(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte index = p.Unpack8();

            r.Mail.TakeAttachment(index);
        }

        #endregion
    }
}
