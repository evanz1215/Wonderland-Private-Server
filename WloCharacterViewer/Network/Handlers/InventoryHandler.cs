using System;
using WloCharacterViewer.Models;

namespace WloCharacterViewer.Network.Handlers
{
    /// <summary>
    /// Handles AC 23 sub 5 (bag inventory).
    /// Format repeating: [slot:8][itemId:16][amount:8][damage:8][24 zero bytes]
    /// </summary>
    public class InventoryBagHandler : IPacketHandler
    {
        private const int EntrySize = 1 + 2 + 1 + 1 + 24; // 29 bytes per item

        public void Handle(PacketReader reader, GameSession session)
        {
            try
            {
                session.Inventory.Bag.Clear();

                while (reader.Remaining >= EntrySize)
                {
                    byte slot = reader.Unpack8();
                    ushort itemId = reader.Unpack16();
                    byte amount = reader.Unpack8();
                    byte damage = reader.Unpack8();
                    reader.UnpackBytes(24); // skip padding

                    if (itemId == 0) continue;

                    session.Inventory.Bag.Add(new InventoryItem
                    {
                        Slot = slot,
                        ItemId = itemId,
                        Quantity = amount
                    });
                }

                session.Log($"[Inventory] Bag loaded: {session.Inventory.Bag.Count} items");
            }
            catch (Exception ex)
            {
                session.Log($"[InventoryBagHandler] Parse error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Handles AC 23 sub 11 (equipped items).
    /// Format repeating: [itemId:16][damage:8][16 zero bytes] for slots 1-6.
    /// </summary>
    public class InventoryEquipHandler : IPacketHandler
    {
        private const int EntrySize = 2 + 1 + 16; // 19 bytes per equip slot

        public void Handle(PacketReader reader, GameSession session)
        {
            try
            {
                session.Inventory.Equipment.Clear();
                byte slot = 1;

                while (reader.Remaining >= EntrySize && slot <= 6)
                {
                    ushort itemId = reader.Unpack16();
                    byte damage = reader.Unpack8();
                    reader.UnpackBytes(16); // skip padding

                    if (itemId != 0)
                    {
                        session.Inventory.Equipment.Add(new InventoryItem
                        {
                            Slot = slot,
                            ItemId = itemId,
                            Quantity = 1
                        });
                    }

                    slot++;
                }

                session.Log($"[Inventory] Equipment loaded: {session.Inventory.Equipment.Count} items");
            }
            catch (Exception ex)
            {
                session.Log($"[InventoryEquipHandler] Parse error: {ex.Message}");
            }
        }
    }
}
