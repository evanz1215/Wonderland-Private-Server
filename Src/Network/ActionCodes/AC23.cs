using System;
using Network;
using Game;

namespace Network.ActionCodes
{
    public class AC23 : AC
    {
        public override int ID { get { return 23; } }

        /// <summary>
        /// AC23 — Inventory actions.
        /// Sub 10 (move), 11 (equip), 12 (unequip), 15 (use), 124 (destroy)
        /// are already handled by EquipManager.ProcessSocket and Inventory.ProcessSocket
        /// in Player.ProcessSocket, so we only handle sub 2 (pickup) and sub 3 (drop) here.
        /// </summary>
        public override void ProcessPkt(Player r, RecievePacket p)
        {
            byte sub = p.Unpack8();

            switch (sub)
            {
                case 2: RecvPickup(r, p); break;
                case 3: RecvDrop(r, p); break;
                // Sub 10, 11, 12, 15, 124 handled by Inventory/EquipManager ProcessSocket
            }
        }

        /// <summary>
        /// Sub 2 — Pick up item from ground
        /// </summary>
        void RecvPickup(Player p, RecievePacket r)
        {
            try
            {
                byte pos = r.Unpack8();
                var map = p.CurMap as GameMap;
                if (map != null)
                    map.onItemPickup(p, pos);
            }
            catch (Exception t) { DebugSystem.Write("[AC23] Pickup error: " + t.Message); }
        }

        /// <summary>
        /// Sub 3 — Drop item from inventory to ground
        /// </summary>
        void RecvDrop(Player p, RecievePacket r)
        {
            try
            {
                byte pos = r.Unpack8();
                byte qnt = r.Unpack8();
                byte ukn = r.Unpack8();
                var item = p.Inv[pos];

                if (item != null && item.ItemID > 0)
                {
                    if (item.Dropable)
                    {
                        var map = p.CurMap as GameMap;
                        if (map != null)
                            map.onItemDrop(p, pos, qnt);
                    }
                    else
                    {
                        // Item is not dropable — ask client to confirm destroy
                        p.Send(Tools.FromFormat("bbbbWb", 23, 212, 255, pos, item.ItemID, qnt));
                    }
                }
            }
            catch (Exception t) { DebugSystem.Write("[AC23] Drop error: " + t.Message); }
        }
    }
}
