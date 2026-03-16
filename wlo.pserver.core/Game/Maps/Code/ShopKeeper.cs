using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network;
using DataFiles;
using Game.Code;

namespace Game.Maps
{
    /// <summary>
    /// NPC ShopKeeper — handles buying items from NPC and selling items to NPC.
    /// Each ShopKeeper has a list of item IDs it sells.
    ///
    /// WLO Shop Protocol:
    ///   AC 27,3 → Server sends shop item list (opens shop UI on client)
    ///   AC 27,1 → Client requests to buy items
    ///   AC 27,2 → Client requests to sell items
    /// </summary>
    public class ShopKeeper : InteractableObjects
    {
        ushort m_clickID;
        string m_name;
        ushort[] m_shopItems;
        PhxItemDat m_itemDat;

        public ShopKeeper() { }

        public ShopKeeper(ushort clickID, string name, PhxItemDat itemDat, params ushort[] shopItemIDs)
        {
            m_clickID = clickID;
            m_name = name;
            m_itemDat = itemDat;
            m_shopItems = shopItemIDs ?? new ushort[0];
        }

        public override MapObjType Type { get { return MapObjType.Npc; } }
        public override ushort CickID { get { return m_clickID; } }
        public string Name { get { return m_name ?? ""; } }
        public ushort[] ShopItems { get { return m_shopItems ?? new ushort[0]; } }

        /// <summary>
        /// Calculate buy price for an item (price player pays to buy from NPC).
        /// Uses item level as base with type multiplier.
        /// </summary>
        public static uint GetBuyPrice(Item item)
        {
            if (item == null || item.ItemID == 0) return 0;
            uint basePrice = (uint)(item.Level + 1) * 100;
            // Equipment is more expensive (Wear_At > 0 means it's equippable)
            if ((byte)item.Wear_At > 0)
                basePrice *= 3;
            return Math.Max(basePrice, 10);
        }

        /// <summary>
        /// Calculate sell price (price NPC pays player). Typically 1/3 of buy price.
        /// </summary>
        public static uint GetSellPrice(Item item)
        {
            return Math.Max(GetBuyPrice(item) / 3, 1);
        }

        /// <summary>
        /// Send the shop item list to the player (AC 27,3).
        /// Opens the shop UI on the client side.
        /// </summary>
        public void SendShopList(Player buyer)
        {
            if (m_itemDat == null || m_shopItems == null || m_shopItems.Length == 0) return;

            SendPacket p = new SendPacket();
            p.Pack8(27);
            p.Pack8(3);
            p.Pack8((byte)m_shopItems.Length);

            foreach (ushort itemID in m_shopItems)
            {
                var itemInfo = m_itemDat.GetItemByID(itemID);
                if (itemInfo != null)
                {
                    Item tmp = new Item(itemInfo);
                    p.Pack16(itemID);
                    p.Pack32(GetBuyPrice(tmp));
                }
            }

            buyer.Send(p);
        }

        /// <summary>
        /// Process a buy request from the player.
        /// Each entry in the cart is: itemIndex (slot in shop list), amount.
        /// </summary>
        public bool ProcessBuy(Player buyer, Code.ShoppingCart[] cart)
        {
            if (m_itemDat == null || cart == null || cart.Length == 0) return false;

            // Calculate total cost first
            uint totalCost = 0;
            var itemsToBuy = new List<KeyValuePair<ushort, byte>>();

            foreach (var entry in cart)
            {
                if (entry.slot >= m_shopItems.Length || entry.ammt == 0) continue;

                ushort itemID = m_shopItems[entry.slot];
                var itemInfo = m_itemDat.GetItemByID(itemID);
                if (itemInfo == null) continue;

                Item tmp = new Item(itemInfo);
                uint price = GetBuyPrice(tmp) * entry.ammt;
                totalCost += price;
                itemsToBuy.Add(new KeyValuePair<ushort, byte>(itemID, entry.ammt));
            }

            // Check if player has enough gold
            if (buyer.Gold < totalCost)
            {
                // Send error: not enough gold (AC 27,5 = buy failed)
                buyer.Send(Tools.FromFormat("bb", 27, 5));
                return false;
            }

            // Check if player has enough inventory space
            if (buyer.Inv.unFilledCount < itemsToBuy.Sum(x => x.Value))
            {
                buyer.Send(Tools.FromFormat("bb", 27, 5));
                return false;
            }

            // Deduct gold and add items
            if (!buyer.Eqs.TakeGold((int)totalCost))
            {
                buyer.Send(Tools.FromFormat("bb", 27, 5));
                return false;
            }

            foreach (var kvp in itemsToBuy)
            {
                buyer.Inv.AddItem(kvp.Key, kvp.Value);
            }

            // Send gold update
            buyer.Eqs.SendGold();

            // Send buy success (AC 27,4)
            buyer.Send(Tools.FromFormat("bb", 27, 4));
            return true;
        }

        /// <summary>
        /// Process a sell request from the player.
        /// Each entry in the cart is: inventorySlot, amount.
        /// </summary>
        public static bool ProcessSell(Player seller, Code.ShoppingCart[] cart)
        {
            if (cart == null || cart.Length == 0) return false;

            uint totalGold = 0;
            var itemsToSell = new List<KeyValuePair<byte, byte>>();

            // Validate all items first
            foreach (var entry in cart)
            {
                if (entry.slot < 1 || entry.slot > 50 || entry.ammt == 0) continue;

                var item = seller.Inv[entry.slot];
                if (item == null || item.ItemID == 0) continue;

                byte actualAmmt = Math.Min(entry.ammt, item.Ammt);
                uint sellPrice = GetSellPrice(item) * actualAmmt;
                totalGold += sellPrice;
                itemsToSell.Add(new KeyValuePair<byte, byte>(entry.slot, actualAmmt));
            }

            if (itemsToSell.Count == 0) return false;

            // Remove items and add gold
            foreach (var kvp in itemsToSell)
            {
                seller.Inv.RemoveItem(kvp.Key, kvp.Value);
            }

            seller.Eqs.AddGold((int)totalGold);
            seller.Eqs.SendGold();

            // Send sell success (AC 27,4)
            seller.Send(Tools.FromFormat("bb", 27, 4));
            return true;
        }

        /// <summary>
        /// Called when a player clicks on this NPC to interact.
        /// Sends the shop list to open the shop UI.
        /// </summary>
        public override void Interact(Player src)
        {
            SendShopList(src);
        }

        public override void Interact(Player src, byte? answer = null)
        {
            SendShopList(src);
        }

        public override void Interact(Player src, byte? answer = null, params Code.ShoppingCart[] items)
        {
            if (items != null && items.Length > 0)
                ProcessBuy(src, items);
            else
                SendShopList(src);
        }
    }
}
