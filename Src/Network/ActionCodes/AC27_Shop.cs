using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Maps;
using Game.Maps.Code;
using Network;

namespace Network.ActionCodes
{
    /// <summary>
    /// AC 27 — Shop/Store Actions
    /// Sub 1: Buy items from NPC shop
    /// Sub 2: Sell items to NPC shop
    ///
    /// Buy packet format:  [27][1][count:byte] then for each: [shopSlot:byte][amount:byte]
    /// Sell packet format: [27][2][count:byte] then for each: [invSlot:byte][amount:byte]
    /// </summary>
    public class AC27_Shop : AC
    {
        public override int ID { get { return 27; } }

        public override void ProcessPkt(Player c, RecievePacket p)
        {
            switch (p.B)
            {
                case 1: Recv_Buy(c, p); break;
                case 2: Recv_Sell(c, p); break;
                default: DebugSystem.Write("AC 27," + p.B + " has not been coded"); break;
            }
        }

        /// <summary>
        /// Player wants to buy items from the shop
        /// </summary>
        void Recv_Buy(Player r, RecievePacket p)
        {
            p.SetPtr(6);

            byte count = p.Unpack8();
            if (count == 0 || count > 20) return;

            ShoppingCart[] cart = new ShoppingCart[count];
            for (int i = 0; i < count; i++)
            {
                cart[i].slot = p.Unpack8();
                cart[i].ammt = p.Unpack8();
            }

            // Find the shop the player is interacting with
            var shop = r.InteractingShop;
            if (shop == null)
            {
                r.Send(Tools.FromFormat("bb", 27, 5));
                return;
            }

            shop.ProcessBuy(r, cart);
        }

        /// <summary>
        /// Player wants to sell items to the shop
        /// </summary>
        void Recv_Sell(Player r, RecievePacket p)
        {
            p.SetPtr(6);

            byte count = p.Unpack8();
            if (count == 0 || count > 20) return;

            ShoppingCart[] cart = new ShoppingCart[count];
            for (int i = 0; i < count; i++)
            {
                cart[i].slot = p.Unpack8();
                cart[i].ammt = p.Unpack8();
            }

            ShopKeeper.ProcessSell(r, cart);
        }
    }
}
