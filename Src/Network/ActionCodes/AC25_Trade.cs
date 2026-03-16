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
    /// AC 25 — Trade Actions
    /// Sub 1: Request trade (client → server)
    /// Sub 2: Accept/decline trade request
    /// Sub 3: Confirm offer (gold + item slots)
    /// Sub 4: Finalize trade
    /// Sub 5: Cancel trade
    /// </summary>
    public class AC25_Trade : AC
    {
        public override int ID { get { return 25; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                case 1: Recv_RequestTrade(c, p); break;
                case 2: Recv_AcceptDecline(c, p); break;
                case 3: Recv_ConfirmOffer(c, p); break;
                case 4: Recv_Finalize(c, p); break;
                case 5: Recv_Cancel(c, p); break;
                default: DebugSystem.Write("AC 25," + p.B + " has not been coded"); break;
            }
        }

        /// <summary>
        /// Player requests to trade with another player
        /// Packet: [25][1][targetCharID:uint32]
        /// </summary>
        void Recv_RequestTrade(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            uint targetID = p.Unpack32();

            Player target = FindPlayerByID(r, targetID);
            if (target == null) return;

            r.Trade.RequestTrade(target);
        }

        /// <summary>
        /// Player accepts or declines a trade request
        /// Packet: [25][2][answer:byte][requesterCharID:uint32]
        /// answer: 1 = accept, 0 = decline
        /// </summary>
        void Recv_AcceptDecline(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            byte answer = p.Unpack8();
            uint requesterID = p.Unpack32();

            if (answer != 1) return;

            Player requester = FindPlayerByID(r, requesterID);
            if (requester == null) return;

            r.Trade.AcceptTrade(requester);
        }

        /// <summary>
        /// Player confirms their trade offer
        /// Packet: [25][3][gold:uint32][count:byte][itemSlot:byte]...
        /// </summary>
        void Recv_ConfirmOffer(Player r, RecievePacket p)
        {
            p.SetPtr(6);
            uint gold = p.Unpack32();

            byte count = p.Unpack8();
            if (count > 20) return;

            List<byte> slots = new List<byte>();
            for (int i = 0; i < count; i++)
            {
                slots.Add(p.Unpack8());
            }

            r.Trade.ConfirmOffer(gold, slots);
        }

        /// <summary>
        /// Player finalizes the trade (agrees to execute)
        /// Packet: [25][4]
        /// </summary>
        void Recv_Finalize(Player r, RecievePacket p)
        {
            r.Trade.FinalizeTrade();
        }

        /// <summary>
        /// Player cancels the trade
        /// Packet: [25][5]
        /// </summary>
        void Recv_Cancel(Player r, RecievePacket p)
        {
            r.Trade.CancelTrade();
        }

        Player FindPlayerByID(Player src, uint charID)
        {
            if (src.CurMap == null) return null;
            var map = src.CurMap as GameMap;
            if (map == null) return null;
            return map.FindPlayer(charID);
        }
    }
}
