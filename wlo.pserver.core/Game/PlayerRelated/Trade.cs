using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network;

namespace Game
{
    /// <summary>
    /// Manages player-to-player trading.
    ///
    /// Trade flow:
    /// 1. Player A requests trade with Player B → AC 25,1
    /// 2. Player B accepts/declines → AC 25,2
    /// 3. Both get trade windows open → AC 25,1 (server→client, opens window)
    /// 4. Each player confirms their offer (gold + items) → AC 25,3
    /// 5. Partner sees the confirmed offer → AC 25,3 (server→client)
    /// 6. Both finalize → AC 25,4 → items/gold swapped
    /// 7. Trade ends → AC 25,2 code 4
    /// </summary>
    public class TradeManager
    {
        Player m_owner;
        Player m_partner;

        uint m_offeredGold;
        List<byte> m_offeredSlots;       // inventory slots owner is offering
        bool m_confirmed;                // owner has locked in their offer
        bool m_finalized;                // owner agreed to complete

        public Player Partner { get { return m_partner; } }
        public bool IsTrading { get { return m_partner != null; } }

        public TradeManager(Player owner)
        {
            m_owner = owner;
            m_offeredSlots = new List<byte>();
        }

        /// <summary>
        /// Send a trade request to the target player.
        /// Server sends AC 25,1 to target with requester's CharID.
        /// </summary>
        public void RequestTrade(Player target)
        {
            if (IsTrading)
            {
                // Already in a trade
                return;
            }
            if (target.Trade.IsTrading)
            {
                // Target is already trading with someone
                return;
            }
            if (!m_owner.Settings.TRADABLE || !target.Settings.TRADABLE)
            {
                // One of the players has trading disabled
                return;
            }

            // Send trade request to target
            SendPacket pkt = new SendPacket();
            pkt.Pack8(25);
            pkt.Pack8(1);
            pkt.Pack32(m_owner.CharID);
            target.Send(pkt);
        }

        /// <summary>
        /// Accept a trade request from requester.
        /// Opens trade windows on both sides (AC 25,1 with partner CharID).
        /// </summary>
        public void AcceptTrade(Player requester)
        {
            if (IsTrading || requester.Trade.IsTrading)
                return;

            // Link both players
            m_partner = requester;
            requester.Trade.m_partner = m_owner;

            // Reset trade state
            ResetOfferState();
            requester.Trade.ResetOfferState();

            // Send open trade window to both
            // Owner gets requester's CharID
            SendPacket toOwner = new SendPacket();
            toOwner.Pack8(25);
            toOwner.Pack8(1);
            toOwner.Pack32(requester.CharID);
            m_owner.Send(toOwner);

            // Requester gets owner's CharID
            SendPacket toRequester = new SendPacket();
            toRequester.Pack8(25);
            toRequester.Pack8(1);
            toRequester.Pack32(m_owner.CharID);
            requester.Send(toRequester);
        }

        /// <summary>
        /// Cancel the current trade. Notifies both players with AC 25,2 code 3.
        /// </summary>
        public void CancelTrade()
        {
            if (!IsTrading) return;

            Player partner = m_partner;

            // Clear both sides
            ClearTradeState();
            if (partner != null && partner.Trade != null)
                partner.Trade.ClearTradeState();

            // Notify both players
            SendPacket cancel = new SendPacket();
            cancel.Pack8(25);
            cancel.Pack8(2);
            cancel.Pack8(3);

            m_owner.Send(cancel);
            if (partner != null)
                partner.Send(cancel);
        }

        /// <summary>
        /// Owner confirms their offer: gold amount and inventory slots.
        /// Sends AC 25,3 to partner with item details.
        /// </summary>
        public void ConfirmOffer(uint gold, List<byte> itemSlots)
        {
            if (!IsTrading) return;

            // Validate gold
            if (gold > 0 && m_owner.Gold < gold)
                return;

            // Validate item slots
            foreach (byte slot in itemSlots)
            {
                var item = m_owner.Inv[slot];
                if (item == null || item.ItemID == 0)
                    return;
            }

            m_offeredGold = gold;
            m_offeredSlots = new List<byte>(itemSlots);
            m_confirmed = true;

            // Reset finalized state if re-confirming
            m_finalized = false;
            m_partner.Trade.m_finalized = false;

            // Send the offer details to partner: AC 25,3
            SendPacket pkt = new SendPacket();
            pkt.Pack8(25);
            pkt.Pack8(3);
            pkt.Pack32(gold);

            foreach (byte slot in itemSlots)
            {
                var item = m_owner.Inv[slot];
                if (item != null)
                {
                    pkt.Pack16(item.ItemID);
                    pkt.Pack8(item.Ammt);
                    pkt.Pack8(item.Damage);
                    // 24 bytes padding for item attributes
                    for (int i = 0; i < 24; i++)
                        pkt.Pack8(0);
                }
            }

            m_partner.Send(pkt);
        }

        /// <summary>
        /// Owner finalizes the trade. If both have finalized, execute the swap.
        /// </summary>
        public void FinalizeTrade()
        {
            if (!IsTrading || !m_confirmed) return;

            m_finalized = true;

            if (m_finalized && m_partner.Trade.m_finalized)
            {
                ExecuteTrade();
            }
        }

        /// <summary>
        /// Execute the actual item/gold swap between both players.
        /// </summary>
        void ExecuteTrade()
        {
            Player partnerPlayer = m_partner;
            TradeManager partnerTrade = m_partner.Trade;

            // --- Remove items from both players first ---

            // Collect item IDs and amounts before removing
            List<KeyValuePair<ushort, byte>> ownerItems = new List<KeyValuePair<ushort, byte>>();
            foreach (byte slot in m_offeredSlots)
            {
                var invItem = m_owner.Inv[slot];
                if (invItem != null && invItem.ItemID != 0)
                {
                    ownerItems.Add(new KeyValuePair<ushort, byte>(invItem.ItemID, invItem.Ammt));
                }
            }

            List<KeyValuePair<ushort, byte>> partnerItems = new List<KeyValuePair<ushort, byte>>();
            foreach (byte slot in partnerTrade.m_offeredSlots)
            {
                var invItem = partnerPlayer.Inv[slot];
                if (invItem != null && invItem.ItemID != 0)
                {
                    partnerItems.Add(new KeyValuePair<ushort, byte>(invItem.ItemID, invItem.Ammt));
                }
            }

            // Remove items from owner
            foreach (byte slot in m_offeredSlots)
            {
                var invItem = m_owner.Inv[slot];
                if (invItem != null && invItem.ItemID != 0)
                    m_owner.Inv.RemoveItem(slot, invItem.Ammt);
            }

            // Remove items from partner
            foreach (byte slot in partnerTrade.m_offeredSlots)
            {
                var invItem = partnerPlayer.Inv[slot];
                if (invItem != null && invItem.ItemID != 0)
                    partnerPlayer.Inv.RemoveItem(slot, invItem.Ammt);
            }

            // --- Add items to receiving players ---

            // Partner receives owner's items
            foreach (var item in ownerItems)
            {
                partnerPlayer.Inv.AddItem(item.Key, item.Value);
            }

            // Owner receives partner's items
            foreach (var item in partnerItems)
            {
                m_owner.Inv.AddItem(item.Key, item.Value);
            }

            // --- Handle gold ---

            if (m_offeredGold > 0)
            {
                m_owner.Eqs.TakeGold((int)m_offeredGold);
                partnerPlayer.Eqs.AddGold((int)m_offeredGold);
            }

            if (partnerTrade.m_offeredGold > 0)
            {
                partnerPlayer.Eqs.TakeGold((int)partnerTrade.m_offeredGold);
                m_owner.Eqs.AddGold((int)partnerTrade.m_offeredGold);
            }

            // Send gold updates
            m_owner.Eqs.SendGold();
            partnerPlayer.Eqs.SendGold();

            // --- Send trade complete to both (AC 25,2 code 4) ---

            SendPacket complete = new SendPacket();
            complete.Pack8(25);
            complete.Pack8(2);
            complete.Pack8(4);

            m_owner.Send(complete);
            partnerPlayer.Send(complete);

            // Clear trade state
            ClearTradeState();
            partnerTrade.ClearTradeState();
        }

        void ResetOfferState()
        {
            m_offeredGold = 0;
            m_offeredSlots.Clear();
            m_confirmed = false;
            m_finalized = false;
        }

        void ClearTradeState()
        {
            m_partner = null;
            ResetOfferState();
        }
    }
}
