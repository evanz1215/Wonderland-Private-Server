using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Network;
using Server;
using Server.System;

namespace Network.ActionCodes
{
    /// <summary>
    /// AC 75 — Item Mall (Cash Shop)
    /// Sub 1: Client sends purchase request / Server sends item list
    /// Sub 3: Server sends purchase result (IM balance + cost + item)
    /// Sub 4: Server sends purchase confirmation per item
    /// Sub 7: Server sends mall enable flag
    /// Sub 8: Server sends mall status
    ///
    /// Item list format (AC 75,1):
    ///   [count:u16] then for each item:
    ///   [itemID:u16][ammt:u8][price:u16][discount%:u8][state:u8][tab:u8][entryId:u8][0:u8]
    ///   Tab: 1=Weaponry, 2=Armory, 3=HOT, 4=Grocery, 5=Furniture
    /// </summary>
    public class AC75_Mall : AC
    {
        public override int ID { get { return 75; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                case 1: Recv_Buy(c, p); break;
                default: DebugSystem.Write("AC 75," + p.B + " has not been coded"); break;
            }
        }

        /// <summary>
        /// AC 75,1 — dual purpose:
        ///   Client sends short packet (no data after sub) → request mall item list
        ///   Client sends purchase data → buy items
        /// Format (buy): [count:u8] then for each: [itemID:u16][ammt:u8][tab:u8][entry:u16]
        /// </summary>
        void Recv_Buy(Player r, RecievePacket p)
        {
            // AC 75,1 is dual-purpose: client sends short packet to request item list,
            // or sends purchase data. A valid buy needs at least:
            //   [header:2][len:2][AC:1][sub:1][count:1][item:6] = 13 bytes
            // If packet is too short, treat as a list request.
            int pktLen = p.Buffer.Count();
            if (pktLen < 13)
            {
                r.Send(BuildMallListPacket());
                return;
            }

            p.SetPtr(6);
            byte count = p.Unpack8();
            if (count == 0 || count > 10) return;

            // Validate packet has enough data for all items
            // Each item entry = 6 bytes (itemID:2 + ammt:1 + tab:1 + entry:2)
            int expectedLen = 7 + (count * 6); // header(4) + AC(1) + sub(1) + count(1) + items
            if (pktLen < expectedLen)
            {
                DebugSystem.Write(string.Format("[AC75] Packet too short: count={0} expected={1} actual={2}", count, expectedLen, pktLen));
                return;
            }

            DebugSystem.Write(string.Format("[AC75] Buy request from player {0}, count={1}", r.CharID, count));

            var mallItems = cGlobal.gImMallManager.Items;
            int totalCost = 0;

            // First pass: validate all items and calculate total cost
            var purchases = new List<ImMallItem>();
            for (int i = 0; i < count; i++)
            {
                ushort itemID = p.Unpack16();
                byte ammt = p.Unpack8();
                byte tab = p.Unpack8();
                ushort entry = p.Unpack16();

                DebugSystem.Write(string.Format("[AC75] Item: id={0} ammt={1} tab={2} entry={3}", itemID, ammt, tab, entry));

                // Find item in mall
                ImMallItem mallItem = null;
                foreach (var mi in mallItems)
                {
                    if (mi.ItemID == itemID && mi.Tab == tab)
                    {
                        mallItem = mi;
                        break;
                    }
                }

                if (mallItem == null)
                {
                    DebugSystem.Write(string.Format("[AC75] Item {0} not found in mall", itemID));
                    return; // Invalid item, abort entire purchase
                }

                int itemCost = (int)(mallItem.Price * mallItem.Discount / 100);
                totalCost += itemCost;
                purchases.Add(mallItem);
            }

            // Check if player has enough IM
            if (r.UserAcc.IM < totalCost)
            {
                DebugSystem.Write(string.Format("[AC75] Player {0} insufficient IM: has {1}, needs {2}",
                    r.CharID, r.UserAcc.IM, totalCost));
                // Send failure — just refresh their IM balance
                r.Send(Tools.FromFormat("bbdddd", 35, 4, r.UserAcc.IM, 0, 0, 0));
                return;
            }

            // Deduct IM
            r.UserAcc.IM -= totalCost;

            // Give items and send per-item confirmations (AC 75,4)
            foreach (var mallItem in purchases)
            {
                r.Inv.AddItem(mallItem.ItemID, mallItem.Amount);
                GameLogger.LogItemGain(r, mallItem.ItemID, mallItem.Amount, "ItemMall");

                // AC 75,4 — per-item purchase confirmation
                r.Send(Tools.FromFormat("bbWbbb", 75, 4, mallItem.ItemID, mallItem.Amount, mallItem.Tab, 0));

                DebugSystem.Write(string.Format("[AC75] Gave player {0} item {1} x{2}",
                    r.CharID, mallItem.ItemID, mallItem.Amount));
            }

            // Save IM to database
            cGlobal.gUserDataBase.UpdateUser(r.UserAcc.DataBaseID, im: r.UserAcc.IM);

            // AC 75,3 — purchase result confirmation (tells client to clear cart)
            r.Send(Tools.FromFormat("bbdddd", 75, 3, r.UserAcc.IM, totalCost, 0, 0));

            // Also send updated IM balance via AC 35,4
            r.Send(Tools.FromFormat("bbdddd", 35, 4, r.UserAcc.IM, 0, 0, 0));

            DebugSystem.Write(string.Format("[AC75] Purchase complete for player {0}: cost={1}, remaining IM={2}",
                r.CharID, totalCost, r.UserAcc.IM));
        }

        /// <summary>
        /// Builds the mall item list packet from ImMallManager.
        /// </summary>
        public static SendPacket BuildMallListPacket()
        {
            return cGlobal.gImMallManager.BuildPacket();
        }
    }
}
